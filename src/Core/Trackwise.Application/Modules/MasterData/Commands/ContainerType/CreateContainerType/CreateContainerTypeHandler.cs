using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateContainerTypeHandler(
    ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateContainerTypeCommand, Result<ContainerTypeDto>>
{
    public async Task<Result<ContainerTypeDto>> Handle(
        CreateContainerTypeCommand req, CancellationToken ct)
    {
        var code = ContainerType.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = ContainerType.NormalizeCode(
                await codes.NextAsync(nameof(ContainerType), "CTY", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted type is allowed, but the steward should be told what they are reusing
            // rather than silently colliding with history.
            var clash = await tenantUow.Repository<ContainerType>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Code == code, ct);

            if (clash is not null)
            {
                return Result<ContainerTypeDto>.Fail(clash.DeletedAt is null
                    ? $"A container type with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted container type. Restore that type or choose another code.");
            }
        }

        var iso = ContainerType.NormalizeIsoSizeTypeCode(req.IsoSizeTypeCode);
        var isoHolder = await ContainerTypeIsoLookup.FindHolderAsync(tenantUow, iso, null, ct);

        if (isoHolder is not null)
        {
            return Result<ContainerTypeDto>.Fail(
                $"ISO size-type code '{iso}' already belongs to '{isoHolder.Name}' ({isoHolder.Code}).");
        }

        var containerType = new ContainerType
        {
            Code = code,
            Name = req.Name,
            IsoSizeTypeCode = iso,
            Category = req.Category,
            LengthFeet = req.LengthFeet,
            IsHighCube = req.IsHighCube,

            // Cleared rather than carried when the type has no length. A TEU factor on a road
            // tanker is a capacity figure nobody established, and it would later be counted.
            TeuFactor = req.LengthFeet.HasValue ? req.TeuFactor : null,

            Notes = req.Notes
        };

        await tenantUow.Repository<ContainerType>().AddAsync(containerType, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<ContainerTypeDto>.Ok(containerType.ToDto());
    }
}
