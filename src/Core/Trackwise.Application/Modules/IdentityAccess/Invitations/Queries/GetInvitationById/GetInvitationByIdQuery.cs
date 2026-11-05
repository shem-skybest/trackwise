using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Invitations.Queries;

public class GetInvitationByIdQuery : IQuery<Result<InvitationDto>>
{
    public Guid Id { get; set; }
}
