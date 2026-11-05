using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

internal sealed class BulkAssignLoadsHandler(ITenantUnitOfWork tenantUow) : IAppRequestHandler<BulkAssignLoadsCommand, Result>
{
    public async Task<Result> Handle(BulkAssignLoadsCommand req, CancellationToken ct)
    {
        var loads = await tenantUow.Repository<Load>()
            .GetListAsync(l => req.LoadIds.Contains(l.Id), ct);

        if (loads.Count == 0)
        {
            return Result.Fail("No loads found with the provided IDs");
        }

        foreach (var load in loads)
        {
            load.AssignedTruckId = req.TruckId;
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
