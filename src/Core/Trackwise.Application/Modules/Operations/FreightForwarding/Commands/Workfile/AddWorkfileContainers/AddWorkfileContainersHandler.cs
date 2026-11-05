using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.FreightForwarding.Containers;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class AddWorkfileContainersHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<AddWorkfileContainersCommand, Result>
{
    public async Task<Result> Handle(AddWorkfileContainersCommand req, CancellationToken ct)
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

        IReadOnlyList<WorkfileContainer> added;
        try
        {
            added = workfile.AddContainers(spec.Value!, req.Quantity);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        foreach (var row in added)
        {
            await tenantUow.Repository<WorkfileContainer>().AddAsync(row, ct);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
