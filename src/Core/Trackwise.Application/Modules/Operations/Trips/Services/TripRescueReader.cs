using Trackwise.Application.Modules.IdentityAccess.Employees;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Trips.Services;

/// <summary>Rescues with their truck, crew, trip and incident named, for a trip page or an incident page.</summary>
internal static class TripRescueReader
{
    public static Task<IReadOnlyList<TripRescueDto>> ForTripAsync(ITenantUnitOfWork uow, Guid tripId, CancellationToken ct) =>
        ReadAsync(uow, uow.Repository<TripRescue>().Query().Where(r => r.TripId == tripId), ct);

    public static Task<IReadOnlyList<TripRescueDto>> ForIncidentAsync(
        ITenantUnitOfWork uow, Guid incidentId, CancellationToken ct) =>
        ReadAsync(uow, uow.Repository<TripRescue>().Query().Where(r => r.IncidentId == incidentId), ct);

    private static async Task<IReadOnlyList<TripRescueDto>> ReadAsync(
        ITenantUnitOfWork uow, IQueryable<TripRescue> query, CancellationToken ct)
    {
        var rescues = await query.OrderBy(r => r.TransferredAt).ToListAsync(ct);
        if (rescues.Count == 0)
        {
            return [];
        }

        var rescueIds = rescues.Select(r => r.Id).ToList();
        var stopsByRescue = (await uow.Repository<TripStop>().Query()
                .Where(s => s.RescueId != null && rescueIds.Contains(s.RescueId.Value))
                .OrderBy(s => s.Order)
                .Select(s => new { s.Id, RescueId = s.RescueId!.Value })
                .ToListAsync(ct))
            .GroupBy(s => s.RescueId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(s => s.Id).ToList());

        var tripIds = rescues.Select(r => r.TripId).Distinct().ToList();
        var tripNumbers = await uow.Repository<Trip>().Query()
            .Where(t => tripIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Number, ct);

        var incidentIds = rescues.Select(r => r.IncidentId).OfType<Guid>().Distinct().ToList();
        var incidentNumbers = incidentIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await uow.Repository<Incident>().Query()
                .Where(i => incidentIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.CaseNumber, ct);

        var trucks = await TruckRefResolver.ResolveAsync(uow, rescues.Select(r => r.TruckId).Distinct().ToList(), ct);
        var drivers = await EmployeeRefResolver.ResolveAsync(
            uow, rescues.Select(r => r.DriverId).OfType<Guid>().Distinct().ToList(), ct);
        var helpers = await HelperRefResolver.ResolveAsync(
            uow, rescues.SelectMany(r => new[] { r.HelperId, r.SecondHelperId }).OfType<Guid>().Distinct().ToList(), ct);

        return rescues
            .Select(r => r.ToDto(
                new RescueRefNames(
                    tripNumbers.GetValueOrDefault(r.TripId),
                    trucks.TryGetValue(r.TruckId, out var truck) ? truck.Number : null,
                    r.DriverId is { } d && drivers.TryGetValue(d, out var driver) ? driver.Name : null,
                    r.HelperId is { } h && helpers.TryGetValue(h, out var helper) ? helper : null,
                    r.SecondHelperId is { } h2 && helpers.TryGetValue(h2, out var second) ? second : null,
                    r.IncidentId is { } i && incidentNumbers.TryGetValue(i, out var number) ? number : null),
                stopsByRescue.GetValueOrDefault(r.Id) ?? []))
            .ToList();
    }
}
