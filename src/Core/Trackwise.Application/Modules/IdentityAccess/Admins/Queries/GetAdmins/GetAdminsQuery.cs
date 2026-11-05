using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Queries;

/// <summary>
/// Lists users that hold an app-level role (SuperAdmin or Admin).
/// </summary>
public sealed class GetAdminsQuery : SearchableQuery, IQuery<PagedResult<UserDto>>
{
}
