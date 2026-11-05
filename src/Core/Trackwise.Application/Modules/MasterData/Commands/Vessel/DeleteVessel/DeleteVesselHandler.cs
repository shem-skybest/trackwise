using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteVesselHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteVesselCommand, Result>
{
    public async Task<Result> Handle(DeleteVesselCommand req, CancellationToken ct)
    {
        var vessel = await tenantUow.Repository<Vessel>().GetByIdAsync(req.Id, ct);
        if (vessel is null)
        {
            return Result.Fail($"Could not find vessel with ID '{req.Id}'");
        }

        // No hand-rolled dependency check, and none needed: the probe runs in the pipeline off the
        // entity's declared rules. Vessel declares none yet - nothing holds a vessel by foreign key
        // while Workfile still carries the name as text - so the probe finds nothing and says so,
        // which is the honest answer rather than a check nobody wrote.
        tenantUow.Repository<Vessel>().Delete(vessel);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
