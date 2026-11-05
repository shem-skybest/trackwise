using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

/// <summary>
/// Reads what <see cref="ContainerTimeline"/> needs - live legs, their loads and the trip stops
/// that moved them - in one query each, then fills each box's status in place.
/// </summary>
internal static class ContainerTimelineReader
{
    public static async Task<List<WorkfileTimelineEntryDto>> ReadAsync(
        ITenantUnitOfWork tenantUow, RegularWorkfile workfile, List<WorkfileContainerDto> containers, CancellationToken ct)
    {
        var vessel = new VesselDates(workfile.Etd, workfile.Eta, workfile.Origin, workfile.Destination);
        var moves = await ReadMovesAsync(tenantUow, workfile.Id, ct);
        return ContainerTimeline.Build(vessel, moves, containers, DateTime.UtcNow);
    }

    private static async Task<List<LegMove>> ReadMovesAsync(ITenantUnitOfWork tenantUow, Guid parentId, CancellationToken ct)
    {
        var legs = await tenantUow.Repository<Workfile>().Query()
            .AsNoTracking()
            .OfType<TruckingWorkfile>()
            .Where(w => w.ParentWorkfileId == parentId && w.Status != WorkfileStatus.Cancelled)
            .Select(w => new { w.Id, w.ReferenceNumber, w.LegRole })
            .ToDictionaryAsync(w => w.Id, ct);

        if (legs.Count == 0)
        {
            return [];
        }

        var legIds = legs.Keys.ToArray();
        var loads = await tenantUow.Repository<Load>().Query()
            .AsNoTracking()
            .Where(l => l.WorkfileId != null && legIds.Contains(l.WorkfileId.Value))
            .Where(l => l.Status != LoadStatus.Cancelled && l.Status != LoadStatus.CancelledByClient)
            .Select(l => new
            {
                l.Id,
                LegId = l.WorkfileId!.Value,
                l.WorkfileContainerId,
                l.OriginTerminalId,
                l.OriginSiteId,
                l.DestinationTerminalId,
                l.DestinationSiteId,
                l.RequestedPickupDate,
                l.PickedUpAt,
                l.DeliveredAt
            })
            .ToListAsync(ct);

        var loadIds = loads.Select(l => l.Id).ToArray();
        var stops = (await tenantUow.Repository<TripStop>().Query()
                .AsNoTracking()
                .Where(s => loadIds.Contains(s.LoadId) && s.Trip.Status != TripStatus.Cancelled)
                .OrderBy(s => s.Order)
                .Select(s => new { s.LoadId, s.Type, s.ArrivedAt, s.DepartedAt, TripNumber = s.Trip.Number })
                .ToListAsync(ct))
            .ToLookup(s => s.LoadId);

        var places = await TruckLegReader.PlaceNamesAsync(
            tenantUow,
            loads.SelectMany(l => new[] { l.OriginTerminalId, l.DestinationTerminalId }),
            loads.SelectMany(l => new[] { l.OriginSiteId, l.DestinationSiteId }),
            ct);

        return loads.Select(l =>
        {
            var leg = legs[l.LegId];
            var pickup = stops[l.Id].FirstOrDefault(s => s.Type == TripStopType.PickUp);
            var drop = stops[l.Id].FirstOrDefault(s => s.Type == TripStopType.DropOff);
            return new LegMove(
                l.LegId,
                leg.ReferenceNumber,
                leg.LegRole!.Value,
                l.WorkfileContainerId,
                pickup?.TripNumber ?? drop?.TripNumber,
                places.GetValueOrDefault(l.OriginTerminalId ?? l.OriginSiteId ?? Guid.Empty),
                l.OriginTerminalId.HasValue,
                places.GetValueOrDefault(l.DestinationTerminalId ?? l.DestinationSiteId ?? Guid.Empty),
                l.DestinationTerminalId.HasValue,
                l.RequestedPickupDate,
                pickup?.ArrivedAt,
                pickup?.DepartedAt ?? l.PickedUpAt,
                drop?.ArrivedAt,
                drop?.DepartedAt ?? l.DeliveredAt);
        }).ToList();
    }
}
