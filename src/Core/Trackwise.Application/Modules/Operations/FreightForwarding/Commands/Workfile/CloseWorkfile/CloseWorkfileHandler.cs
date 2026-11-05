using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CloseWorkfileHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CloseWorkfileCommand, Result>
{
    public async Task<Result> Handle(CloseWorkfileCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.Id, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        if (!WorkfileStatusMachine.CanTransition(workfile.Status, WorkfileStatus.Closed))
        {
            return Result.Fail($"A {workfile.Status} workfile cannot be closed.");
        }

        // Deliberately no IsReferenceLocked guard: a closure request locks the file precisely so
        // that Accounting can finish it, so closing is the one write a locked file still accepts.
        if (!string.IsNullOrWhiteSpace(req.Remarks))
        {
            workfile.SetAccountingRemarks(req.Remarks);
        }

        workfile.Close(req.ClosedBy);

        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
