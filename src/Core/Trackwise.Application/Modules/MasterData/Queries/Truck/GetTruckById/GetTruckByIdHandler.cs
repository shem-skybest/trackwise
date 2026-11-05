using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a truck by id, following the merge chain when the id belongs to a record that has since
/// been folded into another.
/// </summary>
internal sealed class GetTruckByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetTruckByIdQuery, Result<TruckDto>>
{
    public async Task<Result<TruckDto>> Handle(GetTruckByIdQuery req, CancellationToken ct)
    {
        var truck = await tenantUow.Repository<Truck>().GetByIdAsync(req.Id, ct);

        if (truck is not null)
        {
            return Result<TruckDto>.Ok(await ToDtoAsync(truck, ct));
        }

        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Truck), req.Id, ct);

        if (!forwarded)
        {
            return Result<TruckDto>.Fail($"Could not find truck with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Truck>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<TruckDto>.Fail($"Could not find truck with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);
        dto.ForwardedFromId = req.Id;

        return Result<TruckDto>.Ok(dto);
    }

    private async Task<TruckDto> ToDtoAsync(Truck truck, CancellationToken ct) =>
        truck.ToDto(await BranchNameLookup.ResolveAsync(tenantUow, [truck.BranchId], ct));
}
