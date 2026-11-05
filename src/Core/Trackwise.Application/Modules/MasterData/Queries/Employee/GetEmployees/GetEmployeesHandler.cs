using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Queries;

internal sealed class GetEmployeesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetEmployeesQuery, PagedResult<EmployeeDto>>
{
    public async Task<PagedResult<EmployeeDto>> Handle(GetEmployeesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Employee>().Query();

        if (req.IncludeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrEmpty(req.Search))
        {
            var code = Employee.NormalizeCode(req.Search);
            // Lowercased on both sides: Postgres LIKE is case-sensitive.
            var search = req.Search.Trim().ToLower();

            query = query.Where(e => e.Code.Contains(code) || e.Name.ToLower().Contains(search));
        }

        if (req.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == req.IsActive.Value);
        }

        if (req.BranchId is { } branchId)
        {
            // A null branch means "applies everywhere", so narrowing to one branch has to keep the
            // global records - excluding them would hide the shared employees the branch actually uses.
            query = query.Where(e => e.BranchId == branchId || e.BranchId == null);
        }

        if (req.UpdatedSince.HasValue)
        {
            var since = req.UpdatedSince.Value;
            query = query.Where(e => (e.UpdatedAt ?? e.CreatedAt) >= since);
        }

        var totalItems = await query.CountAsync(ct);

        var employees = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var branchNames = await BranchNameLookup.ResolveAsync(
            tenantUow, employees.Select(e => e.BranchId), ct);

        return PagedResult<EmployeeDto>.Ok(
            employees.Select(e => e.ToDto(branchNames)).ToArray(), totalItems, req.PageSize);
    }
}
