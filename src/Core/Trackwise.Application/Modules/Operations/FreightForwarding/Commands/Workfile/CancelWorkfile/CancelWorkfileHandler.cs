using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CancelWorkfileHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CancelWorkfileCommand, Result>
{
    public async Task<Result> Handle(CancelWorkfileCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.Id, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        if (workfile.IsReferenceLocked)
        {
            return Result.Fail(
                $"Workfile '{workfile.ReferenceNumber}' is locked by Accounting and cannot be cancelled.");
        }

        if (!WorkfileStatusMachine.CanTransition(workfile.Status, WorkfileStatus.Cancelled))
        {
            return Result.Fail($"A {workfile.Status} workfile cannot be cancelled.");
        }

        // A leg's loads go with it, or dispatch keeps planning boxes nobody is moving. Once a truck
        // has a box on board the leg happened, and cancelling the paperwork wouldn't undo that.
        var legLoads = workfile is TruckingWorkfile { IsLeg: true }
            ? await tenantUow.Repository<Load>().Query()
                .Where(l => l.WorkfileId == workfile.Id)
                .ToListAsync(ct)
            : [];

        if (legLoads.Any(l => l.Status is LoadStatus.PickedUp or LoadStatus.Delivered))
        {
            return Result.Fail(
                $"A truck has already picked up a container on leg '{workfile.ReferenceNumber}', so it can't be cancelled.");
        }

        foreach (var load in legLoads.Where(l => l.Status is LoadStatus.Draft or LoadStatus.Dispatched))
        {
            load.Cancel();
        }

        workfile.Cancel(req.CancelledBy, req.Reason);

        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
