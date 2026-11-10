using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

/// <summary>Moves a person to another role. The only way a role changes after someone is created or invited.</summary>
public class AssignEmployeeRoleCommand : ICommand
{
    public Guid EmployeeId { get; set; }
    public string RoleName { get; set; } = null!;
}
