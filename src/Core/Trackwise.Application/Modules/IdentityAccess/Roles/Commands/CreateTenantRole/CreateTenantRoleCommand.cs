using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

/// <summary>
/// Creates a role by copying another one's permissions (e.g. "CS + Dispatch Cebu" from
/// "CS + Dispatch"). Returns the new role's name.
/// </summary>
public class CreateTenantRoleCommand : ICommand<Result<string>>
{
    public string DisplayName { get; set; } = null!;
    public string CopyFromRoleName { get; set; } = null!;
}
