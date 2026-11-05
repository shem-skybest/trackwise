using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateContainerTypeHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateContainerTypeCommand, Result>
{
    public async Task<Result> Handle(UpdateContainerTypeCommand req, CancellationToken ct)
    {
        var containerType = await tenantUow.Repository<ContainerType>().GetByIdAsync(req.Id, ct);
        if (containerType is null)
        {
            return Result.Fail($"Could not find container type with ID '{req.Id}'");
        }

        var iso = ContainerType.NormalizeIsoSizeTypeCode(req.IsoSizeTypeCode);
        var isoHolder = await ContainerTypeIsoLookup.FindHolderAsync(
            tenantUow, iso, containerType.Id, ct);

        if (isoHolder is not null)
        {
            return Result.Fail(
                $"ISO size-type code '{iso}' already belongs to '{isoHolder.Name}' ({isoHolder.Code}).");
        }

        containerType.Name = req.Name;
        containerType.IsoSizeTypeCode = iso;
        containerType.Category = req.Category;
        containerType.LengthFeet = req.LengthFeet;
        containerType.IsHighCube = req.IsHighCube;

        // Cleared on the way out of having a length, matching create. A TEU factor left behind on
        // equipment that no longer claims a size is a capacity figure nobody established.
        containerType.TeuFactor = req.LengthFeet.HasValue ? req.TeuFactor : null;

        containerType.Notes = req.Notes;
        containerType.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
