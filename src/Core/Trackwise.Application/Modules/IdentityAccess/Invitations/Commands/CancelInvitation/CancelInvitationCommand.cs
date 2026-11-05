using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Invitations.Commands;

public class CancelInvitationCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
