using Microsoft.EntityFrameworkCore;
using Stripe;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using static System.Net.Mime.MediaTypeNames;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.CreateQuotation;

internal sealed class CreateQuotationHandler(
    ITenantUnitOfWork tenantUow,
    IQuotationRatingEngine ratingEngine,
    IQuotationReferenceGenerator referenceGenerator,
    IFxRateService fxRateService
    )
    : IAppRequestHandler<CreateQuotationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateQuotationCommand req, CancellationToken ct)
    {
        var customerRepo = tenantUow.Repository<Domain.Entities.Customer>();
        var locationRepo = tenantUow.Repository<Location>();
        var quotationRepo = tenantUow.Repository<Quotation>();
        var branchRepo = tenantUow.Repository<Branch>();

        // 1. Verify Customer
        var customer = await customerRepo.GetByIdAsync(req.CustomerId, ct);
        if (customer is null)
            return Result<Guid>.Fail($"Customer with ID '{req.CustomerId}' not found.");

        // 2. Verify Branch
        var branch = await branchRepo.GetByIdAsync(req.BranchId, ct);
        if (branch is null)
            return Result<Guid>.Fail($"Branch with ID '{req.BranchId}' not found.");

        // 2. Reference numbers must be unique per tenant (the DB index is not unique, so enforce it here
        //    and add a unique (TenantId, ReferenceNumber) index to close the race).
        var referenceNumber = await referenceGenerator.NextAsync(req.FreightMode, branch.Code, ct);

        // 3. Normalize and Resolve Route Stops (Multi-Drop or Point-to-Point)
        var stopDtos = QuotationRouteResolver.ResolveStops(req.Lane);
        if (stopDtos.Count < 2)
            return Result<Guid>.Fail("A quote transit route must contain at least two stops (Origin and Destination).");

        // 4. Load & Validate All Stop Locations (single query instead of one per stop)
        var locationIds = stopDtos.Select(s => s.LocationId).Distinct().ToList();
        var locations = await locationRepo.Query()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, ct);

        foreach (var locId in locationIds)
        {
            if (!locations.TryGetValue(locId, out var loc) || !loc.IsActive || loc.DeletedAt.HasValue)
                return Result<Guid>.Fail($"Location '{locId}' is invalid, inactive, or soft-deleted.");

            if (loc.MergedIntoId.HasValue)
                return Result<Guid>.Fail($"Location '{loc.Code}' has been merged into another site and cannot be used.");

            if (loc.Type == LocationType.Zone)
                return Result<Guid>.Fail($"Location '{loc.Code}' is a Zone grouping. Stops must reference physical facilities or delivery points.");

            if (loc.CustomerId.HasValue && loc.CustomerId.Value != req.CustomerId)
                return Result<Guid>.Fail($"Private Location '{loc.Code}' does not belong to Customer '{req.CustomerId}'.");
        }

        // 5. Resolve Route Distance (Multi-Leg Haversine if omitted)
        decimal totalDistanceKm = req.Lane.TotalDistanceKm ?? 0m;
        if (totalDistanceKm <= 0m)
        {
            var calculatedDistance = QuotationRouteResolver.CalculateRouteDistance(stopDtos, locations);
            if (calculatedDistance is null)
            {
                return Result<Guid>.Fail(
                    "Distance could not be auto-calculated because one or more stop locations lack surveyed GPS coordinates. Please specify TotalDistanceKm explicitly.");
            }
            totalDistanceKm = calculatedDistance.Value;
        }

        // 6. Build the aggregate. Domain invariants throw ArgumentException / InvalidOperationException;
        //    surface those as failed Results instead of 500s.
        Quotation quotation;
        try
        {
            var routeStops = stopDtos
                .OrderBy(s => s.Sequence)
                .Select(s => RouteStop.Create(
                    sequence: s.Sequence,
                    locationId: s.LocationId,
                    type: s.Type,
                    windowStart: s.WindowStart,
                    windowEnd: s.WindowEnd,
                    instructions: s.Instructions))
                .ToList();

            var lane = new LaneTransit(routeStops, totalDistanceKm);

            // 6a. Cargo. A "blank" cargo block is treated as no cargo (the validator skips it, and
            //     CargoSpecification.Create would reject an empty commodity).
            CargoSpecification? cargoSpec = null;
            decimal? cargoValuePhp = null;

            if (req.Cargo is not null)
            {
                var cargoCurrency = !string.IsNullOrWhiteSpace(req.Cargo.DeclaredCargoValueCurrency)
                    ? req.Cargo.DeclaredCargoValueCurrency.Trim().ToUpperInvariant()
                    : req.Currency.Trim().ToUpperInvariant(); // fall back to the quotation's currency

                cargoSpec = CargoSpecification.Create(
                    req.Cargo.Commodity,
                    req.Cargo.TotalWeightKg,
                    req.Cargo.PalletCount,
                    new Money { Amount = req.Cargo.DeclaredCargoValueAmount, Currency = cargoCurrency },
                    req.Cargo.RequiresActiveRefrigeration);

                // 6b. The high-value approval rule is evaluated in USD. The USD value is ALWAYS computed
                //     here, server-side. Never accept it from the client, or approval can be bypassed.
                //     (USD cargo is derived by the domain; zero-value cargo needs no conversion.)
                var declared = cargoSpec.DeclaredCargoValue;
                if (declared.Amount > 0m &&
                    !string.Equals(declared.Currency, "PHP", StringComparison.OrdinalIgnoreCase))
                {
                    var rate = await fxRateService.GetRateAsync(declared.Currency, "PHP", ct);
                    if (rate is null or <= 0m)
                    {
                        return Result<Guid>.Fail(
                            $"No {declared.Currency} to PHP exchange rate is available, so the cargo value cannot be assessed for approval.");
                    }

                    cargoValuePhp = Math.Round(declared.Amount * rate.Value, 2);
                }
            }

            TemperatureControl? tempSpec = null;
            if (req.TemperatureSpec is not null)
            {
                tempSpec = TemperatureControl.Create(
                    req.TemperatureSpec.Zone,
                    req.TemperatureSpec.SetpointCelsius,
                    req.TemperatureSpec.AllowableVarianceCelsius,
                    req.TemperatureSpec.Compliance,
                    req.TemperatureSpec.RequiresContinuousTelematics);
            }

            // 7. Instantiate Quotation Aggregate Root
            quotation = Quotation.Create(
                customerId: req.CustomerId,
                branchId: req.BranchId,
                referenceNumber: referenceNumber,
                currency: req.Currency,
                mode: req.Mode,
                freightMode: req.FreightMode,
                shipmentType: req.ShipmentType,
                incoterm: req.Incoterm,
                lane: lane,
                cargoSpec: cargoSpec,
                tempSpec: tempSpec,
                isHazardous: req.IsHazardous,
                requiresCustomsBrokerage: req.RequiresCustomsBrokerage,
                notes: req.Notes,
                declaredCargoValuePhp: cargoValuePhp);

            // 8. Auto-price. See the rating-engine checklist: category from ChargeCode, no fxRate when
            //    currencies match, percentage bases, and the cold-chain / customs lines Submit() demands.
            Guid? customerContractId = null; // Map from customer.ContractId if present on Customer entity
            await ratingEngine.AutoPriceAsync(quotation, customerContractId, ct);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Guid>.Fail(ex.Message);
        }

        // 9. Persist Quotation & Generated QuotationLines
        await quotationRepo.AddAsync(quotation, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(quotation.Id);
    }
}
