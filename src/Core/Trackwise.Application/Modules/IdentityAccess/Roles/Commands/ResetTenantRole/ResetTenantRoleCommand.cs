using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

/// <summary>Puts a built-in role back to its default permissions and lets the migrator sync it again.</summary>
public class ResetTenantRoleCommand : ICommand
{
    public string RoleName { get; set; } = null!;
}
