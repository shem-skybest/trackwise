using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class ClearWorkfileReferenceClosureHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ClearWorkfileReferenceClosureCommand, Result>
{
    public async Task<Result> Handle(ClearWorkfileReferenceClosureCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.Id, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        if (!workfile.IsReferenceLocked)
        {
            return Result.Fail($"Workfile '{workfile.ReferenceNumber}' has no open closure request.");
        }

        if (!string.IsNullOrWhiteSpace(req.Remarks))
        {
            workfile.SetAccountingRemarks(req.Remarks);
        }

        workfile.ReleaseReference();

        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
