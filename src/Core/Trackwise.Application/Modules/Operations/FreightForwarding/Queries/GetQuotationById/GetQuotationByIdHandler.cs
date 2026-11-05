using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Quotations;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetQuotationByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetQuotationByIdQuery, Result<QuotationDto>>
{
    public async Task<Result<QuotationDto>> Handle(GetQuotationByIdQuery req, CancellationToken ct)
    {
        // 1. Query Quotation with eager loading of relational lines ONLY
        // (Lane is read directly from the jsonb column on quotations)
        var quotation = await tenantUow.Repository<Quotation>().Query()
            .Include(q => q.Lines)
                .ThenInclude(l => l.TariffRate)
                    .ThenInclude(tr => tr!.ChargeCode)
            .Include(q => q.Lines)
                .ThenInclude(l => l.TariffRate)
                    .ThenInclude(tr => tr!.Tariff)
            .FirstOrDefaultAsync(q => q.Id == req.Id, ct);

        if (quotation is null)
        {
            return Result<QuotationDto>.Fail($"Quotation with ID '{req.Id}' not found.");
        }

        // 2. Fetch Customer name for read model
        var customer = await tenantUow.Repository<Customer>().GetByIdAsync(quotation.CustomerId, ct);

        // 3. Batch load Location names for route stops safely
        var stops = quotation.Lane?.Stops ?? [];
        var stopLocationIds = stops.Select(s => s.LocationId).Distinct().ToList();

        var locations = stopLocationIds.Count > 0
            ? await tenantUow.Repository<Location>().Query()
                .Where(loc => stopLocationIds.Contains(loc.Id))
                .ToDictionaryAsync(loc => loc.Id, loc => new { loc.Code, loc.Name }, ct)
            : [];

        // 4. Map Route Stops
        var stopsDto = stops
            .OrderBy(s => s.Sequence)
            .Select(s => new RouteStopSummaryDto(
                Sequence: s.Sequence,
                LocationId: s.LocationId,
                LocationCode: locations.TryGetValue(s.LocationId, out var loc) ? loc.Code : "UNKNOWN",
                LocationName: loc?.Name ?? "Unknown Location",
                Type: s.Type,
                WindowStart: s.WindowStart,
                WindowEnd: s.WindowEnd))
            .ToList();

        // 5. Map & Price Quotation Lines
        var linesDto = new List<QuotationLineDto>();
        decimal totalBuying = 0m;
        decimal totalSelling = 0m;

        foreach (var line in quotation.Lines)
        {
            var (buying, selling) = line.Price();

            totalBuying += buying.Amount;
            totalSelling += selling.Amount;

            linesDto.Add(new QuotationLineDto
            {
                Id = line.Id,
                QuotationId = line.QuotationId,
                TariffRateId = line.TariffRateId,
                ChargeCode = line.TariffRate?.ChargeCode?.Code ?? "UNKNOWN",
                ChargeName = line.TariffRate?.ChargeCode?.Name ?? "Unspecified Charge",
                Unit = line.TariffRate?.PricingModel == PricingModel.Percentage
                    ? $"{line.TariffRate.Percentage}{line.TariffRate?.Unit}"
                    : line.TariffRate?.Unit,
                ServiceType = line.ServiceType,
                Quantity = line.Quantity,
                FxRate = line.FxRate,
                MarkupType = line.MarkupType,
                MarkupValue = line.MarkupValue,
                BuyingAmount = buying.Amount,
                SellingAmount = selling.Amount,
                Currency = buying.Currency,
                IsPriceSnapshotted = line.SnapshotBuyingAmount.HasValue,
                Notes = line.Notes
            });
        }

        // 6. Assemble Quotation DTO
        var dto = new QuotationDto
        {
            Id = quotation.Id,
            CustomerId = quotation.CustomerId,
            CustomerName = customer?.Name ?? "Unknown Customer",
            ReferenceNumber = quotation.ReferenceNumber,
            Currency = quotation.Currency,
            Status = quotation.Status,
            ValidUntil = quotation.ValidUntil,
            Mode = quotation.Mode,
            FreightMode = quotation.FreightMode,
            ShipmentType = quotation.ShipmentType,
            Incoterm = quotation.Incoterm,
            TotalDistanceKm = quotation.Lane?.TotalDistanceKm ?? 0m,
            Stops = stopsDto,
            Commodity = quotation.CargoSpec?.Commodity ?? null,
            TotalWeightKg = quotation.CargoSpec?.TotalWeightKg > 0 ? quotation.CargoSpec?.TotalWeightKg : null,
            PalletCount = quotation.CargoSpec?.PalletCount > 0 ? quotation.CargoSpec?.PalletCount : null,
            DeclaredCargoValueAmount = quotation.CargoSpec?.DeclaredCargoValue?.Amount > 0 ? quotation.CargoSpec?.DeclaredCargoValue.Amount : null,
            DeclaredCargoValueCurrency = quotation.CargoSpec?.DeclaredCargoValue.Currency,
            RequiresCustomsBrokerage = quotation.RequiresCustomsBrokerage,
            IsHazardous = quotation.IsHazardous,
            TemperatureSpec = quotation.TemperatureSpec is null ? null : new TemperatureControlDto(
                quotation.TemperatureSpec.Zone,
                quotation.TemperatureSpec.SetpointCelsius,
                quotation.TemperatureSpec.AllowableVarianceCelsius,
                quotation.TemperatureSpec.Compliance,
                quotation.TemperatureSpec.RequiresContinuousTelematics),
            RequiresSpecialApproval = quotation.RequiresSpecialApproval,
            ApprovalEscalationReason = quotation.ApprovalEscalationReason,
            ApprovedBy = quotation.ApprovedBy,
            ApprovedAt = quotation.ApprovedAt,
            TotalBuyingAmount = totalBuying,
            TotalSellingAmount = totalSelling,
            Lines = linesDto,
            Notes = quotation.Notes
        };

        return Result<QuotationDto>.Ok(dto);
    }
}
