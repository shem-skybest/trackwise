using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Queries;

internal sealed class GetFuelSummaryHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetFuelSummaryQuery, Result<FuelSummaryDto>>
{
    /// <summary>Philippine VAT. Pump prices include it; the net amount is what Monday's dispatch board showed per card.</summary>
    private const decimal VatRate = 0.12m;

    public async Task<Result<FuelSummaryDto>> Handle(GetFuelSummaryQuery req, CancellationToken ct)
    {
        var settings = tenantUow.GetCurrentTenant().Settings;
        var fills = await FuelLogReader.FillUps(tenantUow)
            .Apply(req, settings)
            .Select(e => new
            {
                e.Id,
                e.TruckId,
                e.ExpenseDate,
                e.FleetCard,
                e.Status,
                Liters = e.Quantity ?? 0,
                Amount = e.Amount.Amount
            })
            .ToListAsync(ct);

        var currency = settings.Currency.ToString();
        if (fills.Count == 0)
        {
            return Result<FuelSummaryDto>.Ok(new FuelSummaryDto { Currency = currency });
        }

        var truckIds = fills.Select(f => f.TruckId).Distinct().ToList();
        var trucks = await FuelLogReader.TruckNumbersAsync(tenantUow, truckIds, ct);
        var legs = await FuelLogReader.LegsAsync(
            tenantUow, truckIds, fills.Min(f => f.ExpenseDate), fills.Max(f => f.ExpenseDate), ct);

        var liters = fills.Sum(f => f.Liters);
        var amount = fills.Sum(f => f.Amount);

        return Result<FuelSummaryDto>.Ok(new FuelSummaryDto
        {
            FillUps = fills.Count,
            Liters = liters,
            Amount = amount,
            AmountNetOfVat = NetOfVat(amount),
            AveragePricePerLiter = liters > 0 ? Math.Round(amount / liters, 2) : null,
            ForReview = fills.Count(f => f.Status == ExpenseStatus.Pending),
            Flagged = fills.Count(f => f.Status == ExpenseStatus.Rejected),
            Currency = currency,
            ByCard = fills
                .GroupBy(f => f.FleetCard)
                .OrderBy(g => g.Key.HasValue ? (int)g.Key.Value : int.MaxValue)
                .Select(g => new FuelCardTotalDto
                {
                    FleetCard = g.Key,
                    FillUps = g.Count(),
                    Liters = g.Sum(f => f.Liters),
                    Amount = g.Sum(f => f.Amount),
                    AmountNetOfVat = NetOfVat(g.Sum(f => f.Amount))
                })
                .ToList(),
            ByTruck = fills
                .GroupBy(f => f.TruckId)
                .Select(g =>
                {
                    var truckLegs = g.Select(f => legs.GetValueOrDefault(f.Id)).OfType<FuelLeg>().ToList();
                    var km = truckLegs.Sum(l => l.Km);
                    var legLiters = truckLegs.Sum(l => l.Liters);
                    return new FuelTruckTotalDto
                    {
                        TruckId = g.Key,
                        TruckNumber = trucks.GetValueOrDefault(g.Key),
                        FillUps = g.Count(),
                        Liters = g.Sum(f => f.Liters),
                        Amount = g.Sum(f => f.Amount),
                        Km = truckLegs.Count > 0 ? km : null,
                        KmPerLiter = legLiters > 0 ? Math.Round(km / legLiters, 2) : null
                    };
                })
                .OrderBy(t => t.TruckNumber)
                .ToList()
        });
    }

    private static decimal NetOfVat(decimal amount) => Math.Round(amount / (1 + VatRate), 2);
}
