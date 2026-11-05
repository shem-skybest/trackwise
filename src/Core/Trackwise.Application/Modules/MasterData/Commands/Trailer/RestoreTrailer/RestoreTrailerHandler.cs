using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreTrailerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreTrailerCommand, Result>
{
    public async Task<Result> Handle(RestoreTrailerCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var trailer = await tenantUow.Repository<Trailer>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (trailer is null)
        {
            return Result.Fail($"Could not find trailer with ID '{req.Id}'");
        }

        if (trailer.DeletedAt is null)
        {
            return Result.Fail("That trailer is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = trailer.Code;
        var holder = await tenantUow.Repository<Trailer>().Query()
            .FirstOrDefaultAsync(t => t.Code == code && t.Id != trailer.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        // The VIN index is filtered the same way, so the same thing can have happened to it - and
        // unlike a code clash, a VIN clash means the two records are literally the same trailer.
        if (trailer.Vin is { } vin)
        {
            var vinHolder = await tenantUow.Repository<Trailer>().Query()
                .FirstOrDefaultAsync(t => t.Vin == vin && t.Id != trailer.Id, ct);

            if (vinHolder is not null)
            {
                return Result.Fail(
                    $"VIN {vin} is now held by '{vinHolder.Name}' ({vinHolder.Code}), which is the "
                    + "same trailer. Merge this record into that one rather than restoring it.");
            }
        }

        trailer.DeletedAt = null;
        trailer.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
