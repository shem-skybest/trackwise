using Trackwise.Application.Abstractions;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

internal sealed class UpdateContainerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateContainerCommand, Result>
{
    public async Task<Result> Handle(UpdateContainerCommand req, CancellationToken ct)
    {
        var container = await tenantUow.Repository<Container>().GetByIdAsync(req.Id, ct);

        if (container is null)
        {
            return Result.Fail($"Could not find container with ID '{req.Id}'");
        }

        // Canonical vs canonical: a case-only edit is not a change, and the lookup needs the
        // stored form to find anything.
        var number = Container.NormalizeNumber(req.Number);
        if (!string.IsNullOrEmpty(number) && number != container.Number)
        {
            var conflict = await tenantUow.Repository<Container>().GetAsync(i => i.Number == number, ct);
            if (conflict is not null && conflict.Id != container.Id)
            {
                return Result.Fail($"A container with number '{number}' already exists");
            }
        }

        container.Number = PropertyUpdater.UpdateIfChanged(req.Number, container.Number);
        container.ContainerTypeId = PropertyUpdater.UpdateIfChanged(
            req.ContainerTypeId, container.ContainerTypeId);
        container.Notes = PropertyUpdater.UpdateIfChanged(req.Notes, container.Notes);

        tenantUow.Repository<Container>().Update(container);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
