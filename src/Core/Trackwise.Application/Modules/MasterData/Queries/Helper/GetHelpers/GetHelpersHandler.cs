using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetHelpersHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetHelpersQuery, PagedResult<HelperDto>>
{
    public async Task<PagedResult<HelperDto>> Handle(GetHelpersQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Helper>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            var code = Helper.NormalizeCode(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(h => h.Code.Contains(code) || h.Name.ToLower().Contains(search));
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(h => h.IsActive == req.IsActive.Value);
        }

        if (req.AvailableOn is { } day)
        {
            var elsewhere = tenantUow.Repository<CrewStatusEntry>().Query()
                .Where(s => s.Date == day && !CrewDayStatusExtensions.Offered.Contains(s.Status));
            query = query.Where(h => h.IsActive && !elsewhere.Any(s => s.HelperId == h.Id));
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared helpers the branch actually uses.
            query = query.Where(h => h.BranchId == branchId || h.BranchId == null);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;

            // A record created but never edited has a null UpdatedAt, so falling back to CreatedAt
            // is what stops a delta feed silently skipping every untouched new record.
            query = query.Where(h => (h.UpdatedAt ?? h.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var helpers = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, helpers.Select(h => h.BranchId), ct);

        return PagedResult<HelperDto>.Ok(
            helpers.Select(h => h.ToDto(branchNames)).ToArray(), totalItems, req.PageSize);
    }
}
