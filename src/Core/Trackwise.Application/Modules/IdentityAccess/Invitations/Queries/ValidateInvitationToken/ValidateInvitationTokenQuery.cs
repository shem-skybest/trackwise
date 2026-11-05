using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Invitations.Queries;

public class ValidateInvitationTokenQuery : IQuery<Result<InvitationValidationResult>>
{
    public required string Token { get; set; }
}
