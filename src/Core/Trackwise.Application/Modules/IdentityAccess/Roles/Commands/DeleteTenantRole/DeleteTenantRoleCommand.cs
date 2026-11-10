using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

/// <summary>Deletes a role someone created in the app. Built-in roles and roles with people in them stay.</summary>
public class DeleteTenantRoleCommand : ICommand
{
    public string RoleName { get; set; } = null!;
}
