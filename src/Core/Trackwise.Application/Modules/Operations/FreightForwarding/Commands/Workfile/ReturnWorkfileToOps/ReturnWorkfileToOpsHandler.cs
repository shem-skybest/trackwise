using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class ReturnWorkfileToOpsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ReturnWorkfileToOpsCommand, Result>
{
    public async Task<Result> Handle(ReturnWorkfileToOpsCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.Id, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        if (workfile.IsReferenceLocked)
        {
            return Result.Fail(
                $"Workfile '{workfile.ReferenceNumber}' is locked by Accounting and cannot be returned to ops.");
        }

        if (!WorkfileStatusMachine.CanTransition(workfile.Status, WorkfileStatus.Open))
        {
            return Result.Fail($"A {workfile.Status} workfile cannot be returned to ops.");
        }

        workfile.ReturnToOps(req.Reason);

        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
