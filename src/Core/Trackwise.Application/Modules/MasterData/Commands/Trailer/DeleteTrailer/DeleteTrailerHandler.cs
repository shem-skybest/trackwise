using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteTrailerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteTrailerCommand, Result>
{
    public async Task<Result> Handle(DeleteTrailerCommand req, CancellationToken ct)
    {
        var trailer = await tenantUow.Repository<Trailer>().GetByIdAsync(req.Id, ct);
        if (trailer is null)
        {
            return Result.Fail($"Could not find trailer with ID '{req.Id}'");
        }

        // No hand-rolled dependency check, and none needed: the probe runs in the pipeline off the
        // entity's declared rules. Trailer declares none yet - nothing holds a trailer by foreign
        // key until Truck's own promotion wires assignment through it.
        tenantUow.Repository<Trailer>().Delete(trailer);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
