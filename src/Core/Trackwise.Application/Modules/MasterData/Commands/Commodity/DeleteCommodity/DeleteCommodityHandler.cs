using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteCommodityHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteCommodityCommand, Result>
{
    public async Task<Result> Handle(DeleteCommodityCommand req, CancellationToken ct)
    {
        var commodity = await tenantUow.Repository<Commodity>().GetByIdAsync(req.Id, ct);
        if (commodity is null)
        {
            return Result.Fail($"Could not find commodity with ID '{req.Id}'");
        }

        // As with Vessel, the dependency probe runs in the pipeline off the entity's declared
        // rules, and Commodity declares none yet - Workfile still carries the commodity as text.
        tenantUow.Repository<Commodity>().Delete(commodity);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
