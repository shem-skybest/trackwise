using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Queries;

internal sealed class GetTenantRoleChangeLogHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTenantRoleChangeLogQuery, PagedResult<TenantRoleChangeLogDto>>
{
    public Task<PagedResult<TenantRoleChangeLogDto>> Handle(GetTenantRoleChangeLogQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<TenantRoleChangeLog>().Query();

        if (!string.IsNullOrEmpty(req.RoleName))
        {
            query = query.Where(l => l.RoleName == req.RoleName);
        }

        var totalItems = query.Count();
        var items = query
            .OrderByDescending(l => l.ChangedAt)
            .ApplyPaging(req.Page, req.PageSize)
            .Select(l => new TenantRoleChangeLogDto
            {
                Id = l.Id,
                ChangeType = l.ChangeType.ToString(),
                RoleName = l.RoleName,
                RoleDisplayName = l.RoleDisplayName,
                Permission = l.Permission,
                EmployeeId = l.EmployeeId,
                EmployeeName = l.EmployeeName,
                PreviousRoleName = l.PreviousRoleName,
                ChangedByName = l.ChangedByName,
                ChangedAt = l.ChangedAt
            })
            .ToArray();

        return Task.FromResult(PagedResult<TenantRoleChangeLogDto>.Ok(items, totalItems, req.PageSize));
    }
}
