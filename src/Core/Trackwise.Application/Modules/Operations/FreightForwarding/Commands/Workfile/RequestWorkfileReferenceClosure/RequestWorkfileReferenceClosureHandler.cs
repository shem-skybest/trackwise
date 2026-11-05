using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class RequestWorkfileReferenceClosureHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RequestWorkfileReferenceClosureCommand, Result>
{
    public async Task<Result> Handle(RequestWorkfileReferenceClosureCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.Id, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        if (workfile.IsReferenceLocked)
        {
            return Result.Fail(
                $"Workfile '{workfile.ReferenceNumber}' already has a closure request awaiting Accounting.");
        }

        if (workfile.Status is WorkfileStatus.Closed or WorkfileStatus.Cancelled)
        {
            return Result.Fail($"A {workfile.Status} workfile has nothing left to close.");
        }

        workfile.LockReference();

        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
