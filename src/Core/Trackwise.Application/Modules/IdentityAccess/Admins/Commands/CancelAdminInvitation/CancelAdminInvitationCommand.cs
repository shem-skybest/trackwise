using Trackwise.Application.Abstractions.Common;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Commands;

/// <summary>
/// Cancels a pending app-level (Admin) invitation.
/// </summary>
public class CancelAdminInvitationCommand : ICommand
{
    public Guid Id { get; set; }
}
