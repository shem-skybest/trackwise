using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a commodity by id, following the merge chain when the id belongs to a record that has
/// since been folded into another.
/// </summary>
internal sealed class GetCommodityByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetCommodityByIdQuery, Result<CommodityDto>>
{
    public async Task<Result<CommodityDto>> Handle(
        GetCommodityByIdQuery req, CancellationToken ct)
    {
        var commodity = await tenantUow.Repository<Commodity>().GetByIdAsync(req.Id, ct);

        if (commodity is not null)
        {
            return Result<CommodityDto>.Ok(await ToDtoAsync(commodity, ct));
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Commodity), req.Id, ct);

        if (!forwarded)
        {
            return Result<CommodityDto>.Fail($"Could not find commodity with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Commodity>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<CommodityDto>.Fail($"Could not find commodity with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<CommodityDto>.Ok(dto);
    }

    private async Task<CommodityDto> ToDtoAsync(Commodity commodity, CancellationToken ct) =>
        commodity.ToDto(await BranchNameLookup.ResolveAsync(tenantUow, [commodity.BranchId], ct));
}
