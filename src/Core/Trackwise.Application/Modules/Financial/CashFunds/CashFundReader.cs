using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.CashFunds;

/// <summary>
/// A fund with its expenses. Trucks, trips and names come from one lookup each: a Monday half-month
/// runs to a hundred lines, and reading each line's navigations would be a query per line.
/// </summary>
internal static class CashFundReader
{
    public static async Task<CashFundDto> ToDtoAsync(ITenantUnitOfWork tenantUow, CashFund fund, CancellationToken ct)
    {
        var lines = fund.Expenses.OrderBy(e => e.SpentOn).ThenBy(e => e.CreatedAt).ToList();

        var truckIds = lines.Select(e => e.TruckId).OfType<Guid>().Distinct().ToList();
        var tripIds = lines.Select(e => e.TripId).OfType<Guid>().Distinct().ToList();
        var employeeIds = lines.Select(e => e.SpentById).OfType<Guid>().Append(fund.CustodianId).Distinct().ToList();

        var trucks = await tenantUow.Repository<Truck>().Query()
            .IgnoreQueryFilters()
            .Where(t => truckIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Number, ct);
        var trips = await tenantUow.Repository<Trip>().Query()
            .Where(t => tripIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Number, ct);
        var names = await tenantUow.Repository<Employee>().Query()
            .IgnoreQueryFilters()
            .Where(e => employeeIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        var expenses = lines
            .Select(e => e.ToDto() with
            {
                TruckNumber = e.TruckId is { } truckId ? trucks.GetValueOrDefault(truckId) : null,
                TripNumber = e.TripId is { } tripId && trips.TryGetValue(tripId, out var number) ? number : null,
                SpentByName = e.SpentById is { } payerId ? names.GetValueOrDefault(payerId) : null
            })
            .ToList();

        return fund.ToDto(names.GetValueOrDefault(fund.CustodianId), expenses);
    }
}
