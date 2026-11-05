using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreHelperHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreHelperCommand, Result>
{
    public async Task<Result> Handle(RestoreHelperCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var helper = await tenantUow.Repository<Helper>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.Id == req.Id, ct);

        if (helper is null)
        {
            return Result.Fail($"Could not find helper with ID '{req.Id}'");
        }

        if (helper.DeletedAt is null)
        {
            return Result.Fail("That helper is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = helper.Code;
        var holder = await tenantUow.Repository<Helper>().Query()
            .FirstOrDefaultAsync(h => h.Code == code && h.Id != helper.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        helper.DeletedAt = null;
        helper.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
