using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Legs;

/// <summary>The loads behind a truck leg - one per container slot it moves - for adding and editing a leg.</summary>
internal static class TruckLegLoads
{
    /// <summary>
    /// Slots already moved by another live leg in the same role. A box can have both a pickup and a
    /// delivery leg (domestic door to door), but two deliveries of one box is a typing slip; a
    /// cancelled leg frees its boxes.
    /// </summary>
    public static async Task<List<Guid>> SlotsOnOtherLegsAsync(
        ITenantUnitOfWork tenantUow,
        Guid parentId,
        TruckLegRole role,
        List<Guid> slotIds,
        Guid? exceptLegId,
        CancellationToken ct)
    {
        if (slotIds.Count == 0)
        {
            return [];
        }

        var liveLegIds = tenantUow.Repository<Workfile>().Query()
            .OfType<TruckingWorkfile>()
            .Where(w => w.ParentWorkfileId == parentId && w.LegRole == role && w.Status != WorkfileStatus.Cancelled)
            .Where(w => w.Id != exceptLegId)
            .Select(w => w.Id);

        return await tenantUow.Repository<Load>().Query()
            .Where(l => l.WorkfileId != null && liveLegIds.Contains(l.WorkfileId.Value))
            .Where(l => l.WorkfileContainerId != null && slotIds.Contains(l.WorkfileContainerId.Value))
            .Select(l => l.WorkfileContainerId!.Value)
            .Distinct()
            .ToListAsync(ct);
    }

    public static Load Build(
        TruckingWorkfile leg,
        Guid? slotId,
        string? slotLabel,
        ResolvedLegEnd from,
        ResolvedLegEnd to,
        Customer customer,
        Employee? dispatcher,
        TruckLegFields fields)
    {
        var load = Load.Create(
            NameFor(leg, slotLabel),
            LoadType.GeneralFreight,
            0,
            from.Address,
            from.Location,
            to.Address,
            to.Location,
            customer,
            assignedTruck: null,
            dispatcher,
            withInvoice: false);

        load.WorkfileId = leg.Id;
        load.WorkfileContainerId = slotId;
        load.Commodity = leg.Commodity;
        Apply(load, from, to, fields);
        return load;
    }

    /// <summary>Puts an edited leg's ends, date and notes on a load that hasn't gone out yet.</summary>
    public static void Apply(Load load, ResolvedLegEnd from, ResolvedLegEnd to, TruckLegFields fields)
    {
        load.OriginAddress = from.Address;
        load.OriginLocation = from.Location;
        load.OriginTerminalId = from.TerminalId;
        load.OriginSiteId = from.SiteId;
        load.DestinationAddress = to.Address;
        load.DestinationLocation = to.Location;
        load.DestinationTerminalId = to.TerminalId;
        load.DestinationSiteId = to.SiteId;
        load.Distance = from.Location.DistanceTo(to.Location);
        load.RequestedPickupDate = fields.PickupDate;
        load.Notes = fields.Notes;
    }

    public static string NameFor(TruckingWorkfile leg, string? slotLabel) =>
        slotLabel is null ? leg.ReferenceNumber : $"{leg.ReferenceNumber} {slotLabel}";
}
