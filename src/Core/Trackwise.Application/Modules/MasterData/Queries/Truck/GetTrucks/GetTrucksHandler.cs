using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetTrucksHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTrucksQuery, PagedResult<TruckDto>>
{
    public async Task<PagedResult<TruckDto>> Handle(GetTrucksQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Truck>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            var number = Truck.NormalizeCode(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(t => t.Number.Contains(number) || t.Name.ToLower().Contains(search));
        }

        if (req.Type.HasValue)
        {
            query = query.Where(t => t.Type == req.Type.Value);
        }

        if (req.Types?.Length > 0)
        {
            query = query.Where(t => req.Types.Contains(t.Type));
        }

        if (req.Statuses?.Length > 0)
        {
            query = query.Where(t => req.Statuses.Contains(t.Status));
        }

        if (req.MissingGrossVehicleWeight)
        {
            query = query.Where(t => t.GrossVehicleWeightKg == null);
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(t => t.IsActive == req.IsActive.Value);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared trucks the branch actually uses.
            query = query.Where(t => t.BranchId == branchId || t.BranchId == null);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;
            query = query.Where(t => (t.UpdatedAt ?? t.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var trucks = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, trucks.Select(t => t.BranchId), ct);

        return PagedResult<TruckDto>.Ok(
            trucks.Select(t => t.ToDto(branchNames)).ToArray(), totalItems, req.PageSize);
    }
}
