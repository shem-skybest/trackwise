using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

/// <summary>
/// The leg side of a workfile page, each lookup batched: a forwarding file's legs, or a leg's
/// parent and loads. Names read past the soft-delete filters, as every display path does - a
/// retired port or partner still has to show on the leg that used it.
/// </summary>
internal static class TruckLegReader
{
    public static async Task<WorkfileLegsView?> ReadAsync(
        ITenantUnitOfWork tenantUow, Workfile workfile, List<WorkfileContainerDto> containers, CancellationToken ct)
    {
        if (workfile is TruckingWorkfile { ParentWorkfileId: { } parentId } leg)
        {
            return await ReadLegAsync(tenantUow, leg, parentId, ct);
        }

        return workfile.SupportsTruckLegs
            ? new WorkfileLegsView(null, await ReadLegsAsync(tenantUow, workfile.Id, containers, ct), [])
            : null;
    }

    private static async Task<List<TruckLegDto>> ReadLegsAsync(
        ITenantUnitOfWork tenantUow, Guid parentId, List<WorkfileContainerDto> containers, CancellationToken ct)
    {
        var legs = await tenantUow.Repository<Workfile>().Query()
            .AsNoTracking()
            .OfType<TruckingWorkfile>()
            .Where(w => w.ParentWorkfileId == parentId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

        if (legs.Count == 0)
        {
            return [];
        }

        var legIds = legs.Select(l => l.Id).ToArray();
        var loads = await tenantUow.Repository<Load>().Query()
            .AsNoTracking()
            .Where(l => l.WorkfileId != null && legIds.Contains(l.WorkfileId.Value))
            .OrderBy(l => l.Number)
            .Select(l => new
            {
                LegId = l.WorkfileId!.Value,
                l.WorkfileContainerId,
                l.RequestedPickupDate,
                l.OriginTerminalId,
                l.OriginSiteId,
                l.DestinationTerminalId,
                l.DestinationSiteId,
                l.Notes,
                IsPlanned = l.Status == LoadStatus.Draft && !l.TripStops.Any()
            })
            .ToListAsync(ct);

        var places = await PlaceNamesAsync(
            tenantUow,
            loads.SelectMany(l => new[] { l.OriginTerminalId, l.DestinationTerminalId }),
            loads.SelectMany(l => new[] { l.OriginSiteId, l.DestinationSiteId }),
            ct);

        var truckerIds = legs.Select(l => l.TruckerId).OfType<Guid>().Distinct().ToArray();
        var truckers = truckerIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await tenantUow.Repository<BusinessPartner>().Query()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(p => truckerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var labels = WorkfileContainerReader.Labels(containers);
        var loadsByLeg = loads.ToLookup(l => l.LegId);

        return legs.Select(leg =>
        {
            var legLoads = loadsByLeg[leg.Id].ToList();
            var first = legLoads.FirstOrDefault();
            return new TruckLegDto
            {
                Id = leg.Id,
                ReferenceNumber = leg.ReferenceNumber,
                Role = leg.LegRole!.Value,
                Status = leg.Status,
                From = first is null ? null : places.GetValueOrDefault(first.OriginTerminalId ?? first.OriginSiteId ?? Guid.Empty),
                To = first is null ? null : places.GetValueOrDefault(first.DestinationTerminalId ?? first.DestinationSiteId ?? Guid.Empty),
                TruckerName = leg.TruckerId is { } truckerId ? truckers.GetValueOrDefault(truckerId) : null,
                PickupDate = first?.RequestedPickupDate,
                Containers = legLoads
                    .Select(l => l.WorkfileContainerId is { } slotId ? labels.GetValueOrDefault(slotId) : null)
                    .OfType<string>()
                    .ToList(),
                FromTerminalId = first?.OriginTerminalId,
                FromSiteId = first?.OriginSiteId,
                ToTerminalId = first?.DestinationTerminalId,
                ToSiteId = first?.DestinationSiteId,
                TruckerId = leg.TruckerId,
                ContainerSlotIds = legLoads.Select(l => l.WorkfileContainerId).OfType<Guid>().ToList(),
                Notes = first?.Notes,
                CanEdit = leg.IsEditable && legLoads.All(l => l.IsPlanned)
            };
        }).ToList();
    }

    private static async Task<WorkfileLegsView> ReadLegAsync(
        ITenantUnitOfWork tenantUow, TruckingWorkfile leg, Guid parentId, CancellationToken ct)
    {
        var parentReference = await tenantUow.Repository<Workfile>().Query()
            .AsNoTracking()
            .Where(w => w.Id == parentId)
            .Select(w => w.ReferenceNumber)
            .FirstOrDefaultAsync(ct);

        var loads = await tenantUow.Repository<Load>().Query()
            .AsNoTracking()
            .Where(l => l.WorkfileId == leg.Id)
            .OrderBy(l => l.Number)
            .Select(l => new { l.Id, l.Number, l.Name, l.Status, l.WorkfileContainerId, l.AssignedTruckId })
            .ToListAsync(ct);

        var labels = WorkfileContainerReader.Labels(await WorkfileContainerReader.ReadAsync(tenantUow, parentId, ct));
        var trucks = await TruckRefResolver.ResolveAsync(
            tenantUow, loads.Select(l => l.AssignedTruckId).OfType<Guid>().Distinct().ToArray(), ct);

        var legLoads = loads.Select(l => new TruckLegLoadDto
        {
            Id = l.Id,
            Number = l.Number,
            Name = l.Name,
            Status = l.Status,
            Container = l.WorkfileContainerId is { } slotId ? labels.GetValueOrDefault(slotId) : null,
            TruckNumber = l.AssignedTruckId is { } truckId && trucks.TryGetValue(truckId, out var truck) ? truck.Number : null
        }).ToList();

        return new WorkfileLegsView(parentReference, [], legLoads);
    }

    public static async Task<Dictionary<Guid, string>> PlaceNamesAsync(
        ITenantUnitOfWork tenantUow, IEnumerable<Guid?> terminalIds, IEnumerable<Guid?> siteIds, CancellationToken ct)
    {
        var terminals = await TerminalRefResolver.ResolveAsync(
            tenantUow, terminalIds.OfType<Guid>().Distinct().ToArray(), ct);

        var siteIdList = siteIds.OfType<Guid>().Distinct().ToArray();
        var sites = siteIdList.Length == 0
            ? new Dictionary<Guid, string>()
            : await tenantUow.Repository<Location>().Query()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => siteIdList.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        foreach (var (id, terminal) in terminals)
        {
            sites[id] = terminal.Name;
        }

        return sites;
    }
}
