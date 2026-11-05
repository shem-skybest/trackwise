using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a vessel by id, following the merge chain when the id belongs to a record that has since
/// been folded into another.
/// </summary>
internal sealed class GetVesselByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetVesselByIdQuery, Result<VesselDto>>
{
    public async Task<Result<VesselDto>> Handle(GetVesselByIdQuery req, CancellationToken ct)
    {
        var vessel = await tenantUow.Repository<Vessel>().GetByIdAsync(req.Id, ct);

        if (vessel is not null)
        {
            return Result<VesselDto>.Ok(await ToDtoAsync(vessel, ct));
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Vessel), req.Id, ct);

        if (!forwarded)
        {
            return Result<VesselDto>.Fail($"Could not find vessel with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Vessel>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<VesselDto>.Fail($"Could not find vessel with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<VesselDto>.Ok(dto);
    }

    private async Task<VesselDto> ToDtoAsync(Vessel vessel, CancellationToken ct) =>
        vessel.ToDto(await BranchNameLookup.ResolveAsync(tenantUow, [vessel.BranchId], ct));
}
