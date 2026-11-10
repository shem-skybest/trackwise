using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

/// <summary>Replaces a role's permissions with <see cref="Permissions"/> and marks the role customized.</summary>
public class UpdateTenantRolePermissionsCommand : ICommand
{
    public string RoleName { get; set; } = null!;
    public string[] Permissions { get; set; } = [];
}
