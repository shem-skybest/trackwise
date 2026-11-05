using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreContainerTypeHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreContainerTypeCommand, Result>
{
    public async Task<Result> Handle(RestoreContainerTypeCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var containerType = await tenantUow.Repository<ContainerType>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (containerType is null)
        {
            return Result.Fail($"Could not find container type with ID '{req.Id}'");
        }

        if (containerType.DeletedAt is null)
        {
            return Result.Fail("That container type is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = containerType.Code;
        var holder = await tenantUow.Repository<ContainerType>().Query()
            .FirstOrDefaultAsync(c => c.Code == code && c.Id != containerType.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        // The ISO index is filtered the same way, and unlike the code it can collide even when the
        // code does not - a steward who deleted HC40 and then created a new record carrying 45G1
        // leaves this record unrestorable until one of them gives the code up.
        var isoHolder = await ContainerTypeIsoLookup.FindHolderAsync(
            tenantUow, containerType.IsoSizeTypeCode, containerType.Id, ct);

        if (isoHolder is not null && isoHolder.DeletedAt is null)
        {
            return Result.Fail(
                $"ISO size-type code '{containerType.IsoSizeTypeCode}' is now used by "
                + $"'{isoHolder.Name}' ({isoHolder.Code}). Merge this record into that one instead.");
        }

        containerType.DeletedAt = null;
        containerType.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
