using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class RemoveWorkfileContainerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RemoveWorkfileContainerCommand, Result>
{
    public async Task<Result> Handle(RemoveWorkfileContainerCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.WorkfileId, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.WorkfileId}'");
        }

        var liveLegIds = tenantUow.Repository<Workfile>().Query()
            .Where(w => w.Status != WorkfileStatus.Cancelled)
            .Select(w => w.Id);
        var onLeg = await tenantUow.Repository<Load>().Query()
            .AnyAsync(l => l.WorkfileContainerId == req.Id && liveLegIds.Contains(l.WorkfileId!.Value), ct);
        if (onLeg)
        {
            return Result.Fail("A truck leg is moving this container. Cancel that leg first.");
        }

        WorkfileContainer removed;
        try
        {
            removed = workfile.RemoveContainer(req.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        tenantUow.Repository<WorkfileContainer>().Delete(removed);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
