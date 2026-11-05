using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog;

/// <summary>
/// Fill-ups are the truck expenses in the fuel category. Truck and trip numbers come from one lookup
/// each, and the distance per fill-up from one read of the trucks' readings, not a query per row.
/// </summary>
internal static class FuelLogReader
{
    /// <summary>How far back a truck's previous reading is looked for. After two months with no fill-up the next reading starts afresh.</summary>
    private static readonly TimeSpan Lookback = TimeSpan.FromDays(60);

    public static IQueryable<TruckExpense> FillUps(ITenantUnitOfWork tenantUow) =>
        tenantUow.Repository<Expense>().Query()
            .OfType<TruckExpense>()
            .Where(e => e.Category == TruckExpenseCategory.Fuel);

    /// <summary>The legs closed by the trucks' fill-ups between <paramref name="from"/> and <paramref name="to"/>.</summary>
    public static async Task<Dictionary<Guid, FuelLeg>> LegsAsync(
        ITenantUnitOfWork tenantUow, IReadOnlyCollection<Guid> truckIds, DateTime from, DateTime to, CancellationToken ct)
    {
        var start = from - Lookback;
        var readings = await FillUps(tenantUow)
            .Where(e => truckIds.Contains(e.TruckId) && e.ExpenseDate >= start && e.ExpenseDate <= to)
            .Select(e => new FuelReading(e.Id, e.TruckId, e.ExpenseDate, e.Number, e.Quantity ?? 0, e.OdometerReading))
            .ToListAsync(ct);

        return FuelEconomy.Legs(readings);
    }

    public static Task<Dictionary<Guid, string>> TruckNumbersAsync(
        ITenantUnitOfWork tenantUow, IReadOnlyCollection<Guid> truckIds, CancellationToken ct) =>
        tenantUow.Repository<Truck>().Query()
            .IgnoreQueryFilters()
            .Where(t => truckIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Number, ct);

    public static async Task<IReadOnlyList<FuelFillUpDto>> ToDtosAsync(
        ITenantUnitOfWork tenantUow, IReadOnlyList<TruckExpense> fills, CancellationToken ct)
    {
        if (fills.Count == 0)
        {
            return [];
        }

        var truckIds = fills.Select(f => f.TruckId).Distinct().ToList();
        var tripIds = fills.Select(f => f.TripId).OfType<Guid>().Distinct().ToList();

        var trucks = await TruckNumbersAsync(tenantUow, truckIds, ct);
        var trips = await tenantUow.Repository<Trip>().Query()
            .Where(t => tripIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Number, ct);
        var legs = await LegsAsync(
            tenantUow, truckIds, fills.Min(f => f.ExpenseDate), fills.Max(f => f.ExpenseDate), ct);

        return fills
            .Select(f =>
            {
                var leg = legs.GetValueOrDefault(f.Id);
                long? tripNumber = f.TripId is { } tripId && trips.TryGetValue(tripId, out var number) ? number : null;
                return f.ToFuelFillUpDto(trucks.GetValueOrDefault(f.TruckId), tripNumber, leg?.Km, leg?.KmPerLiter);
            })
            .ToList();
    }
}
