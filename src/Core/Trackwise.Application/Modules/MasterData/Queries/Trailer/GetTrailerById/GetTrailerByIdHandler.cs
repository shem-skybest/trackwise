using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a trailer by id, following the merge chain when the id belongs to a record that has since
/// been folded into another.
/// </summary>
internal sealed class GetTrailerByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetTrailerByIdQuery, Result<TrailerDto>>
{
    public async Task<Result<TrailerDto>> Handle(GetTrailerByIdQuery req, CancellationToken ct)
    {
        var trailer = await tenantUow.Repository<Trailer>().GetByIdAsync(req.Id, ct);

        if (trailer is not null)
        {
            return Result<TrailerDto>.Ok(await ToDtoAsync(trailer, ct));
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Trailer), req.Id, ct);

        if (!forwarded)
        {
            return Result<TrailerDto>.Fail($"Could not find trailer with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Trailer>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<TrailerDto>.Fail($"Could not find trailer with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<TrailerDto>.Ok(dto);
    }

    private async Task<TrailerDto> ToDtoAsync(Trailer trailer, CancellationToken ct) =>
        trailer.ToDto(await BranchNameLookup.ResolveAsync(tenantUow, [trailer.BranchId], ct));
}
