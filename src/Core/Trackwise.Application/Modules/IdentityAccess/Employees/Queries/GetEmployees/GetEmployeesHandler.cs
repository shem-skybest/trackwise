using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Queries;

internal sealed class GetEmployeesHandler(ITenantUnitOfWork tenantUow) : IAppRequestHandler<GetEmployeesQuery, PagedResult<EmployeeDto>>
{
    public Task<PagedResult<EmployeeDto>> Handle(
        GetEmployeesQuery req,
        CancellationToken ct)
    {
        var query = tenantUow.Repository<Employee>().Query();

        if (!string.IsNullOrEmpty(req.Search))
        {
            // Lowercased on both sides: Postgres LIKE is case-sensitive. Name is first + last, so
            // "mark alvero" matches too.
            var search = req.Search.Trim().ToLower();
            query = query.Where(i =>
                i.Name.ToLower().Contains(search) ||
                (i.PhoneNumber != null && i.PhoneNumber.Contains(search)) ||
                i.Email.ToLower().Contains(search));
        }

        if (req.Roles.Length > 0)
        {
            query = query.Where(e => e.Role != null && req.Roles.Contains(e.Role.Name));
        }

        if (req.AvailableOn is { } day)
        {
            var elsewhere = tenantUow.Repository<CrewStatusEntry>().Query()
                .Where(s => s.Date == day && !CrewDayStatusExtensions.Offered.Contains(s.Status));
            query = query.Where(e => e.IsActive && !elsewhere.Any(s => s.EmployeeId == e.Id));
        }

        var totalItems = query.Count();

        var employeeDto = query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .Select(employeeEntity => employeeEntity.ToDto())
            .ToArray();

        return Task.FromResult(PagedResult<EmployeeDto>.Ok(employeeDto, totalItems, req.PageSize));
    }
}
