using Trackwise.Application.Abstractions.Common;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Commands;

/// <summary>
/// Resends a pending app-level (Admin) invitation email.
/// </summary>
public class ResendAdminInvitationCommand : ICommand
{
    public Guid Id { get; set; }
}
