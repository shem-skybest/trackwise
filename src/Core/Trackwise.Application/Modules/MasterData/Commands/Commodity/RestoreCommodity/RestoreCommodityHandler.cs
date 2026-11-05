using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreCommodityHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreCommodityCommand, Result>
{
    public async Task<Result> Handle(RestoreCommodityCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var commodity = await tenantUow.Repository<Commodity>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (commodity is null)
        {
            return Result.Fail($"Could not find commodity with ID '{req.Id}'");
        }

        if (commodity.DeletedAt is null)
        {
            return Result.Fail("That commodity is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = commodity.Code;
        var holder = await tenantUow.Repository<Commodity>().Query()
            .FirstOrDefaultAsync(c => c.Code == code && c.Id != commodity.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        commodity.DeletedAt = null;
        commodity.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
