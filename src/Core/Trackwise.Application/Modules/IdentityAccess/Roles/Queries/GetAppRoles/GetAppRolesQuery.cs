using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Queries;

public class GetAppRolesQuery : SearchableQuery, IQuery<PagedResult<RoleDto>>
{
}
