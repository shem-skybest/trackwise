using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Geocoding;
using Trackwise.Application.Modules.Operations.FreightForwarding.Legs;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class UpdateTruckLegHandler(ITenantUnitOfWork tenantUow, IGeocodingService geocoding)
    : IAppRequestHandler<UpdateTruckLegCommand, Result>
{
    public async Task<Result> Handle(UpdateTruckLegCommand req, CancellationToken ct)
    {
        var leg = await tenantUow.Repository<Workfile>().GetByIdAsync(req.LegId, ct) as TruckingWorkfile;
        if (leg is null || leg.ParentWorkfileId != req.WorkfileId)
        {
            return Result.Fail($"Could not find truck leg with ID '{req.LegId}'");
        }

        var parent = await tenantUow.Repository<Workfile>().GetByIdAsync(req.WorkfileId, ct);
        if (parent is null || !parent.IsEditable)
        {
            return Result.Fail($"Workfile '{parent?.ReferenceNumber}' can't be edited in its current state.");
        }

        var loads = await tenantUow.Repository<Load>().Query()
            .Where(l => l.WorkfileId == leg.Id)
            .ToListAsync(ct);

        // Once dispatch has a load on a trip, the truck, crew and stops were planned around these
        // ends; changing them here would leave the trip pointing at the old ones.
        var loadIds = loads.Select(l => l.Id).ToArray();
        var onTrip = await tenantUow.Repository<TripStop>().Query().AnyAsync(s => loadIds.Contains(s.LoadId), ct);
        if (onTrip || loads.Any(l => l.Status != LoadStatus.Draft))
        {
            return Result.Fail(
                $"Leg '{leg.ReferenceNumber}' is already with dispatch. Change it there, or cancel it and add a new leg.");
        }

        var labels = WorkfileContainerReader.Labels(await WorkfileContainerReader.ReadAsync(tenantUow, parent.Id, ct));
        if (req.ContainerSlotIds.Any(id => !labels.ContainsKey(id)))
        {
            return Result.Fail($"That container is not on workfile '{parent.ReferenceNumber}'.");
        }

        var taken = await TruckLegLoads.SlotsOnOtherLegsAsync(
            tenantUow, parent.Id, req.Role, req.ContainerSlotIds, exceptLegId: leg.Id, ct);
        if (taken.Count > 0)
        {
            return Result.Fail(
                $"Already on a {req.Role.GetDescription().ToLowerInvariant()} leg: "
                + string.Join(", ", taken.Select(id => labels[id])) + ".");
        }

        var partnerCheck = await WorkfilePartnerValidation.ValidateAsync(
            tenantUow.Repository<BusinessPartner>(), new WorkfileHeaderData { TruckerId = req.TruckerId }, ct);
        if (!partnerCheck.IsSuccess)
        {
            return Result.Fail(partnerCheck.Error!);
        }

        var from = await TruckLegEnds.ResolveAsync(tenantUow, geocoding, req.From, parent.CustomerId, ct);
        if (!from.IsSuccess)
        {
            return Result.Fail(from.Error!);
        }

        var to = await TruckLegEnds.ResolveAsync(tenantUow, geocoding, req.To, parent.CustomerId, ct);
        if (!to.IsSuccess)
        {
            return Result.Fail(to.Error!);
        }

        try
        {
            leg.UpdateLeg(req.Role, req.TruckerId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        var wanted = req.ContainerSlotIds.Count == 0 ? [null] : req.ContainerSlotIds.Cast<Guid?>().ToList();

        foreach (var load in loads.Where(l => !wanted.Contains(l.WorkfileContainerId)))
        {
            tenantUow.Repository<Load>().Delete(load);
        }

        foreach (var load in loads.Where(l => wanted.Contains(l.WorkfileContainerId)))
        {
            TruckLegLoads.Apply(load, from.Value!, to.Value!, req);
        }

        var added = wanted.Where(slotId => loads.All(l => l.WorkfileContainerId != slotId)).ToList();
        if (added.Count > 0)
        {
            var customer = await tenantUow.Repository<Customer>().GetByIdAsync(leg.CustomerId, ct);
            if (customer is null)
            {
                return Result.Fail($"Workfile '{parent.ReferenceNumber}' has no customer record.");
            }

            var dispatcher = loads.FirstOrDefault()?.AssignedDispatcherId is { } dispatcherId
                ? await tenantUow.Repository<Employee>().GetByIdAsync(dispatcherId, ct)
                : null;

            await tenantUow.Repository<Load>().AddRangeAsync(
                added.Select(slotId => TruckLegLoads.Build(
                    leg, slotId, slotId is { } id ? labels[id] : null, from.Value!, to.Value!, customer, dispatcher, req)),
                ct);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
