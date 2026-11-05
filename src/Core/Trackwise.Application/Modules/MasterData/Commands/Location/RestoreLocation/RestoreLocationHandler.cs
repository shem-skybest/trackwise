using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreLocationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreLocationCommand, Result>
{
    public async Task<Result> Handle(RestoreLocationCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var location = await tenantUow.Repository<Location>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == req.Id, ct);

        if (location is null)
        {
            return Result.Fail($"Could not find location with ID '{req.Id}'");
        }

        if (location.DeletedAt is null)
        {
            return Result.Fail("That location is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = location.Code;
        var holder = await tenantUow.Repository<Location>().Query()
            .FirstOrDefaultAsync(l => l.Code == code && l.Id != location.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        // The parent link survived the delete, so the zone it points at may have been deleted or
        // merged away since. Restoring into a zone that is no longer there would put the record in
        // a tree nothing can walk, so it comes back unfiled and the steward re-files it.
        if (location.ParentLocationId is { } parentId)
        {
            var parentLives = await tenantUow.Repository<Location>().Query()
                .AnyAsync(l => l.Id == parentId, ct);

            if (!parentLives)
            {
                location.ParentLocationId = null;
            }
        }

        location.DeletedAt = null;
        location.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
