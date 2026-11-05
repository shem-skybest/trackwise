using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Containers;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class UpdateWorkfileContainerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateWorkfileContainerCommand, Result>
{
    public async Task<Result> Handle(UpdateWorkfileContainerCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.WorkfileId, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.WorkfileId}'");
        }

        var spec = await WorkfileContainerBoxes.ResolveAsync(tenantUow, req, ct);
        if (!spec.IsSuccess)
        {
            return Result.Fail(spec.Error!);
        }

        try
        {
            workfile.UpdateContainer(req.Id, spec.Value!);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
