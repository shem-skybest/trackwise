using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteContainerTypeHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteContainerTypeCommand, Result>
{
    public async Task<Result> Handle(DeleteContainerTypeCommand req, CancellationToken ct)
    {
        var containerType = await tenantUow.Repository<ContainerType>().GetByIdAsync(req.Id, ct);
        if (containerType is null)
        {
            return Result.Fail($"Could not find container type with ID '{req.Id}'");
        }

        // The dependency probe has already run in the pipeline off this entity's declared rules -
        // containers, bookings and tariffs - and refused the delete unless the steward overrode it
        // with a reason.
        tenantUow.Repository<ContainerType>().Delete(containerType);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
