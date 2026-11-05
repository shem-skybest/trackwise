using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Queries;

public class GetRolePermissionsQuery : IQuery<Result<PermissionDto[]>>
{
    public string RoleName { get; set; } = null!;
}
