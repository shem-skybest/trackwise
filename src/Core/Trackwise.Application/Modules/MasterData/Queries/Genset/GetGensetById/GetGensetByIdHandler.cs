using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a genset by id, following the merge chain when the id belongs to a record that has since
/// been folded into another.
/// </summary>
internal sealed class GetGensetByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetGensetByIdQuery, Result<GensetDto>>
{
    public async Task<Result<GensetDto>> Handle(GetGensetByIdQuery req, CancellationToken ct)
    {
        var genset = await tenantUow.Repository<Genset>().GetByIdAsync(req.Id, ct);

        if (genset is not null)
        {
            return Result<GensetDto>.Ok(await ToDtoAsync(genset, ct));
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Genset), req.Id, ct);

        if (!forwarded)
        {
            return Result<GensetDto>.Fail($"Could not find genset with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Genset>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<GensetDto>.Fail($"Could not find genset with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<GensetDto>.Ok(dto);
    }

    private async Task<GensetDto> ToDtoAsync(Genset genset, CancellationToken ct) =>
        genset.ToDto(await BranchNameLookup.ResolveAsync(tenantUow, [genset.BranchId], ct));
}
