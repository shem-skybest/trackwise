using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Queries;

/// <summary>Who changed which role or moved whom, newest first. <see cref="RoleName"/> narrows it to one role.</summary>
public class GetTenantRoleChangeLogQuery : PagedQuery, IQuery<PagedResult<TenantRoleChangeLogDto>>
{
    public string? RoleName { get; set; }
}
