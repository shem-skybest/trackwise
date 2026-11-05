using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Queries;

public sealed class GetUsersQuery : SearchableQuery, IQuery<PagedResult<UserDto>>
{
}
