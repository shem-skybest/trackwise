using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.UpdateQuotation;

internal sealed class UpdateQuotationHandler(
    ITenantUnitOfWork tenantUow,
    IQuotationRatingEngine ratingEngine,
    IFxRateService fxRateService)
    : IAppRequestHandler<UpdateQuotationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateQuotationCommand request, CancellationToken ct)
    {
        var req = request.Data;

        var customerRepo = tenantUow.Repository<Customer>();
        var locationRepo = tenantUow.Repository<Location>();
        var quotationRepo = tenantUow.Repository<Quotation>();

        // 1. Load the existing quotation (with lines, since we reprice)
        var quotation = await quotationRepo.Query()
            //.Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == request.Id, ct);

        if (quotation is null)
            return Result<Guid>.Fail($"Quotation '{request.Id}' not found.", ErrorCodes.NotFound);

        // 2. Only drafts are editable
        if (quotation.Status != QuotationStatus.Draft)
            return Result<Guid>.Fail($"Only draft quotations can be edited (current status: {quotation.Status}).", ErrorCodes.QuotationNotDraft);

        // 3. Customer
        if (await customerRepo.GetByIdAsync(req.CustomerId, ct) is null)
            return Result<Guid>.Fail($"Customer with ID '{req.CustomerId}' not found.", ErrorCodes.NotFound);

        // 5. Route
        var stopDtos = QuotationRouteResolver.ResolveStops(req.Lane);
        if (stopDtos.Count < 2)
            return Result<Guid>.Fail("A quote transit route must contain at least two stops (Origin and Destination).", ErrorCodes.RouteTooFewStops);

        var locationIds = stopDtos.Select(s => s.LocationId).Distinct().ToList();
        var locations = await locationRepo.Query()
            .Where(l => locationIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, ct);

        var locationError = QuotationRouteResolver.ValidateLocations(locationIds, locations, req.CustomerId);
        if (locationError is not null)
            return Result<Guid>.Fail(locationError);

        decimal totalDistanceKm = req.Lane.TotalDistanceKm ?? 0m;
        if (totalDistanceKm <= 0m)
        {
            var calculated = QuotationRouteResolver.CalculateRouteDistance(stopDtos, locations);
            if (calculated is null)
                return Result<Guid>.Fail(
                    "Distance could not be auto-calculated because one or more stop locations lack surveyed GPS coordinates. Please specify TotalDistanceKm explicitly.", ErrorCodes.DistanceNotCalculable);
            totalDistanceKm = calculated.Value;
        }

        // 6. Apply changes through the aggregate, then reprice
        try
        {
            var routeStops = stopDtos
                .OrderBy(s => s.Sequence)
                .Select(s => RouteStop.Create(s.Sequence, s.LocationId, s.Type,
                    s.WindowStart, s.WindowEnd, s.Instructions))
                .ToList();

            var lane = new LaneTransit(routeStops, totalDistanceKm);

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
                else
                {
                    cargoValuePhp = declared;
                }
            }

            TemperatureControl? tempSpec = req.TemperatureSpec is null
                ? null
                : TemperatureControl.Create(
                    req.TemperatureSpec.Zone,
                    req.TemperatureSpec.SetpointCelsius,
                    req.TemperatureSpec.AllowableVarianceCelsius,
                    req.TemperatureSpec.Compliance,
                    req.TemperatureSpec.RequiresContinuousTelematics);

            // Clear first: UpdateDetails refuses a currency change while lines exist.
            quotation.ClearLines();
            // ASSUMED domain method: revalidates invariants and replaces the editable details.

            quotation.UpdateDetails(
                customerId: req.CustomerId,
                branchId: req.BranchId,
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
                declaredCargoValuePhp: cargoValuePhp); // same as create, until the FX service is enabled

            await ratingEngine.AutoPriceAsync(quotation, customerContractId: null, ct);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return Result<Guid>.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result<Guid>.Ok(quotation.Id);
    }
}
