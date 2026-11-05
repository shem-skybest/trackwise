using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Commands;

/// <summary>
/// Sets a starting password on an employee's login, replacing any existing one. For staff with no
/// working inbox (drivers sit on placeholder emails), so the forgot-password link can never reach them.
/// </summary>
public class SetEmployeePasswordCommand : ICommand
{
    public Guid UserId { get; set; }
    public string Password { get; set; } = "";
}
