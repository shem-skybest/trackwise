using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a helper by id, following the merge chain when the id belongs to a record that has since
/// been folded into another.
/// </summary>
internal sealed class GetHelperByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetHelperByIdQuery, Result<HelperDto>>
{
    public async Task<Result<HelperDto>> Handle(GetHelperByIdQuery req, CancellationToken ct)
    {
        var helper = await tenantUow.Repository<Helper>().GetByIdAsync(req.Id, ct);

        if (helper is not null)
        {
            return Result<HelperDto>.Ok(await ToDtoAsync(helper, ct));
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Helper), req.Id, ct);

        if (!forwarded)
        {
            return Result<HelperDto>.Fail($"Could not find helper with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Helper>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<HelperDto>.Fail($"Could not find helper with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<HelperDto>.Ok(dto);
    }

    private async Task<HelperDto> ToDtoAsync(Helper helper, CancellationToken ct) =>
        helper.ToDto(await BranchNameLookup.ResolveAsync(tenantUow, [helper.BranchId], ct));
}
