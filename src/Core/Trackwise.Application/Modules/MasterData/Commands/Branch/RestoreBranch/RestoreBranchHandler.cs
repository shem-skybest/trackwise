using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreBranchHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreBranchCommand, Result>
{
    public async Task<Result> Handle(RestoreBranchCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var branch = await tenantUow.Repository<Branch>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == req.Id, ct);

        if (branch is null)
        {
            return Result.Fail($"Could not find branch with ID '{req.Id}'");
        }

        if (branch.DeletedAt is null)
        {
            return Result.Fail("That branch is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = branch.Code;
        var holder = await tenantUow.Repository<Branch>().Query()
            .FirstOrDefaultAsync(b => b.Code == code && b.Id != branch.Id, ct);

        if (holder is not null)
        {
            // A code clash on restore is a merge conflict in miniature, so the message points at
            // the resolution instead of stopping at "cannot be restored".
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        branch.DeletedAt = null;
        branch.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
