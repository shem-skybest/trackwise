using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Invitations.Queries;

public class GetInvitationsQuery : SearchableQuery, IQuery<PagedResult<InvitationDto>>
{
    public InvitationStatus? Status { get; set; }
    public InvitationType? Type { get; set; }
}
