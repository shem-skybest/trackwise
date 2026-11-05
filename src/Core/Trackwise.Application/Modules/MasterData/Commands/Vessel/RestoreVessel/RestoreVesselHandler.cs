using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreVesselHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreVesselCommand, Result>
{
    public async Task<Result> Handle(RestoreVesselCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var vessel = await tenantUow.Repository<Vessel>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct);

        if (vessel is null)
        {
            return Result.Fail($"Could not find vessel with ID '{req.Id}'");
        }

        if (vessel.DeletedAt is null)
        {
            return Result.Fail("That vessel is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = vessel.Code;
        var holder = await tenantUow.Repository<Vessel>().Query()
            .FirstOrDefaultAsync(v => v.Code == code && v.Id != vessel.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        // The IMO index is filtered the same way, so the same thing can have happened to it - and
        // unlike a code clash, an IMO clash means the two records are literally the same ship.
        if (vessel.ImoNumber is { } imo)
        {
            var imoHolder = await tenantUow.Repository<Vessel>().Query()
                .FirstOrDefaultAsync(v => v.ImoNumber == imo && v.Id != vessel.Id, ct);

            if (imoHolder is not null)
            {
                return Result.Fail(
                    $"IMO number {imo} is now held by '{imoHolder.Name}' ({imoHolder.Code}), which "
                    + "is the same ship. Merge this record into that one rather than restoring it.");
            }
        }

        vessel.DeletedAt = null;
        vessel.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
