using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteHelperHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteHelperCommand, Result>
{
    public async Task<Result> Handle(DeleteHelperCommand req, CancellationToken ct)
    {
        var helper = await tenantUow.Repository<Helper>().GetByIdAsync(req.Id, ct);
        if (helper is null)
        {
            return Result.Fail($"Could not find helper with ID '{req.Id}'");
        }

        // No hand-rolled dependency check, and none needed: the probe runs in the pipeline off the
        // entity's declared rules. Helper declares none yet - nothing holds a helper by foreign key
        // until Employee/Driver's own promotion wires assignment through it.
        tenantUow.Repository<Helper>().Delete(helper);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
