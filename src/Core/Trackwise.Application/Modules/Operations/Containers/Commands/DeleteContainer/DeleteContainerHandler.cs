using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

internal sealed class DeleteContainerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteContainerCommand, Result>
{
    public async Task<Result> Handle(DeleteContainerCommand req, CancellationToken ct)
    {
        var container = await tenantUow.Repository<Container>().GetByIdAsync(req.Id, ct);

        if (container is null)
        {
            return Result.Fail($"Could not find container with ID '{req.Id}'");
        }

        var linkedLoad = await tenantUow.Repository<Load>().GetAsync(i => i.ContainerId == container.Id, ct);
        if (linkedLoad is not null)
        {
            return Result.Fail("Container is linked to one or more loads and cannot be deleted");
        }

        var shipped = await tenantUow.Repository<WorkfileContainer>().GetAsync(i => i.ContainerId == container.Id, ct);
        if (shipped is not null)
        {
            return Result.Fail("Container is on one or more workfiles and cannot be deleted");
        }

        tenantUow.Repository<Container>().Delete(container);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
