using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreGensetHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreGensetCommand, Result>
{
    public async Task<Result> Handle(RestoreGensetCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var genset = await tenantUow.Repository<Genset>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.Id == req.Id, ct);

        if (genset is null)
        {
            return Result.Fail($"Could not find genset with ID '{req.Id}'");
        }

        if (genset.DeletedAt is null)
        {
            return Result.Fail("That genset is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = genset.Code;
        var holder = await tenantUow.Repository<Genset>().Query()
            .FirstOrDefaultAsync(g => g.Code == code && g.Id != genset.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        // The serial-number index is filtered the same way, so the same thing can have happened to
        // it - and unlike a code clash, a serial-number clash means the two records are literally
        // the same genset.
        if (genset.SerialNumber is { } serialNumber)
        {
            var serialHolder = await tenantUow.Repository<Genset>().Query()
                .FirstOrDefaultAsync(g => g.SerialNumber == serialNumber && g.Id != genset.Id, ct);

            if (serialHolder is not null)
            {
                return Result.Fail(
                    $"Serial number {serialNumber} is now held by '{serialHolder.Name}' "
                    + $"({serialHolder.Code}), which is the same genset. Merge this record into "
                    + "that one rather than restoring it.");
            }
        }

        genset.DeletedAt = null;
        genset.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
