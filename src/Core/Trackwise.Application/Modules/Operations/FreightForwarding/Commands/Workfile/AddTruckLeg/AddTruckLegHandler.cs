using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Geocoding;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.FreightForwarding.Legs;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class AddTruckLegHandler(
    ITenantUnitOfWork tenantUow,
    IWorkfileReferenceGenerator referenceGenerator,
    IGeocodingService geocoding,
    ICurrentUserService currentUser)
    : IAppRequestHandler<AddTruckLegCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AddTruckLegCommand req, CancellationToken ct)
    {
        var parent = await tenantUow.Repository<Workfile>().GetByIdAsync(req.WorkfileId, ct);
        if (parent is null)
        {
            return Result<Guid>.Fail($"Could not find workfile with ID '{req.WorkfileId}'");
        }

        if (!parent.SupportsTruckLegs || !parent.IsEditable)
        {
            return Result<Guid>.Fail(parent.SupportsTruckLegs
                ? $"Workfile '{parent.ReferenceNumber}' can't be edited in its current state."
                : $"Workfile '{parent.ReferenceNumber}' can't have truck legs.");
        }

        var slots = await WorkfileContainerReader.ReadAsync(tenantUow, parent.Id, ct);
        var labels = WorkfileContainerReader.Labels(slots);

        if (req.ContainerSlotIds.Any(id => !labels.ContainsKey(id)))
        {
            return Result<Guid>.Fail($"That container is not on workfile '{parent.ReferenceNumber}'.");
        }

        var taken = await TruckLegLoads.SlotsOnOtherLegsAsync(
            tenantUow, parent.Id, req.Role, req.ContainerSlotIds, exceptLegId: null, ct);
        if (taken.Count > 0)
        {
            return Result<Guid>.Fail(
                $"Already on a {req.Role.GetDescription().ToLowerInvariant()} leg: "
                + string.Join(", ", taken.Select(id => labels[id])) + ".");
        }

        var header = new WorkfileHeaderData { TruckerId = req.TruckerId, Commodity = parent.Commodity };
        var partnerCheck = await WorkfilePartnerValidation.ValidateAsync(
            tenantUow.Repository<BusinessPartner>(), header, ct);
        if (!partnerCheck.IsSuccess)
        {
            return Result<Guid>.Fail(partnerCheck.Error!);
        }

        var from = await TruckLegEnds.ResolveAsync(tenantUow, geocoding, req.From, parent.CustomerId, ct);
        if (!from.IsSuccess)
        {
            return Result<Guid>.Fail(from.Error!);
        }

        var to = await TruckLegEnds.ResolveAsync(tenantUow, geocoding, req.To, parent.CustomerId, ct);
        if (!to.IsSuccess)
        {
            return Result<Guid>.Fail(to.Error!);
        }

        var customer = await tenantUow.Repository<Customer>().GetByIdAsync(parent.CustomerId, ct);
        if (customer is null)
        {
            return Result<Guid>.Fail($"Workfile '{parent.ReferenceNumber}' has no customer record.");
        }

        // The person planning the leg is its dispatcher until dispatch reassigns it; an account with
        // no employee record (a platform admin) plans it unassigned.
        var dispatcher = currentUser.GetUserId() is { } userId
            ? await tenantUow.Repository<Employee>().GetByIdAsync(userId, ct)
            : null;

        var reference = await referenceGenerator.NextAsync(WorkfileSeries.TruckLeg, ct);
        var leg = TruckingWorkfile.CreateLeg(reference, parent, req.Role, header);

        var slotIds = req.ContainerSlotIds.Count == 0 ? [null] : req.ContainerSlotIds.Cast<Guid?>().ToList();
        var loads = slotIds
            .Select(slotId => TruckLegLoads.Build(
                leg, slotId, slotId is { } id ? labels[id] : null, from.Value!, to.Value!, customer, dispatcher, req))
            .ToList();

        await tenantUow.Repository<Workfile>().AddAsync(leg, ct);
        await tenantUow.Repository<Load>().AddRangeAsync(loads, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<Guid>.Ok(leg.Id);
    }
}
