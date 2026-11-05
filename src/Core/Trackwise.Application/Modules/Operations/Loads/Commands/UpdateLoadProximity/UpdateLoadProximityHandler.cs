using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

internal sealed class UpdateLoadProximityHandler(ITenantUnitOfWork tenantUow, ICurrentUserService currentUser)
    : IAppRequestHandler<UpdateLoadProximityCommand, Result>
{
    public async Task<Result> Handle(UpdateLoadProximityCommand req, CancellationToken ct)
    {
        var load = await tenantUow.Repository<Load>().GetByIdAsync(req.LoadId);

        if (load is null)
        {
            return Result.Fail($"Could not find load with ID '{req.LoadId}'");
        }

        if (!await DriverLoadAccess.CanActOnAsync(tenantUow, currentUser.GetUserId(), load.Id, ct))
        {
            return Result.Fail(DriverLoadAccess.NotYourLoad);
        }

        // UpdateProximity raises LoadProximityChangedEvent for notifications
        load.UpdateProximity(req.IsInProximity);

        tenantUow.Repository<Load>().Update(load);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
