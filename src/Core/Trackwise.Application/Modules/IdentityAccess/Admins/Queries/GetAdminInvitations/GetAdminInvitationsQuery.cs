using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Queries;

/// <summary>
/// Lists pending app-level (Admin) invitations.
/// </summary>
public sealed class GetAdminInvitationsQuery : SearchableQuery, IQuery<PagedResult<InvitationDto>>
{
}
