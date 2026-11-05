using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Commands;

/// <summary>
/// Adds a user as an app-level Admin. If a user with the given email already exists,
/// the Admin role is granted immediately; otherwise an invitation email is sent.
/// </summary>
public class AddAdminCommand : ICommand<Result<AddAdminResult>>
{
    public required string Email { get; set; }
    public string? PersonalMessage { get; set; }
}
