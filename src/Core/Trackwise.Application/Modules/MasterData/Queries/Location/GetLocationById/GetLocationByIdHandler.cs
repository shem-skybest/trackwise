using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a location by id, following the merge chain when the id belongs to a record that has since
/// been folded into another.
/// </summary>
internal sealed class GetLocationByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetLocationByIdQuery, Result<LocationDto>>
{
    public async Task<Result<LocationDto>> Handle(GetLocationByIdQuery req, CancellationToken ct)
    {
        var location = await tenantUow.Repository<Location>().GetByIdAsync(req.Id, ct);

        if (location is not null)
        {
            return Result<LocationDto>.Ok(await ToDtoAsync(location, ct));
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Location), req.Id, ct);

        if (!forwarded)
        {
            return Result<LocationDto>.Fail($"Could not find location with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Location>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<LocationDto>.Fail($"Could not find location with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<LocationDto>.Ok(dto);
    }

    private async Task<LocationDto> ToDtoAsync(Location location, CancellationToken ct)
    {
        var branchNames = await BranchNameLookup.ResolveAsync(tenantUow, [location.BranchId], ct);
        var parentNames = new Dictionary<Guid, string>();

        if (location.ParentLocationId is { } parentId)
        {
            // Past the filter, matching the list: a location inside a deleted zone should still
            // name it rather than render as unfiled.
            var name = await tenantUow.Repository<Location>().Query()
                .IgnoreQueryFilters()
                .Where(l => l.Id == parentId)
                .Select(l => l.Name)
                .FirstOrDefaultAsync(ct);

            if (name is not null)
            {
                parentNames[parentId] = name;
            }
        }

        var customerNames = await CustomerNameLookup.ResolveAsync(tenantUow, [location.CustomerId], ct);

        return location.ToDto(branchNames, parentNames, customerNames);
    }
}
