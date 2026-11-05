using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a branch by id, following the merge chain when the id belongs to a record that has since
/// been folded into another.
/// <para>
/// This is what makes merging safe: an old booking holding a merged id still resolves, to the
/// survivor, flagged as forwarded rather than silently substituted.
/// </para>
/// </summary>
internal sealed class GetBranchByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetBranchByIdQuery, Result<BranchDto>>
{
    public async Task<Result<BranchDto>> Handle(GetBranchByIdQuery req, CancellationToken ct)
    {
        var branch = await tenantUow.Repository<Branch>().GetByIdAsync(req.Id, ct);

        if (branch is not null)
        {
            return Result<BranchDto>.Ok(branch.ToDto());
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Branch), req.Id, ct);

        if (!forwarded)
        {
            return Result<BranchDto>.Fail($"Could not find branch with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Branch>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<BranchDto>.Fail($"Could not find branch with ID '{req.Id}'");
        }

        var dto = survivor.ToDto();

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<BranchDto>.Ok(dto);
    }
}
