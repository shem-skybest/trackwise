using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a container type by id, following the merge chain when the id belongs to a record that
/// has since been folded into another.
/// </summary>
internal sealed class GetContainerTypeByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetContainerTypeByIdQuery, Result<ContainerTypeDto>>
{
    public async Task<Result<ContainerTypeDto>> Handle(
        GetContainerTypeByIdQuery req, CancellationToken ct)
    {
        var containerType = await tenantUow.Repository<ContainerType>().GetByIdAsync(req.Id, ct);

        if (containerType is not null)
        {
            return Result<ContainerTypeDto>.Ok(containerType.ToDto());
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(ContainerType), req.Id, ct);

        if (!forwarded)
        {
            return Result<ContainerTypeDto>.Fail($"Could not find container type with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<ContainerType>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<ContainerTypeDto>.Fail($"Could not find container type with ID '{req.Id}'");
        }

        var dto = survivor.ToDto();

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<ContainerTypeDto>.Ok(dto);
    }
}
