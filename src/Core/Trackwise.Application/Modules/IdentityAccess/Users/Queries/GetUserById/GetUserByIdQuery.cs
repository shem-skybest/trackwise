using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Queries;

public sealed class GetUserByIdQuery : IQuery<Result<UserDto>>
{
    public required string UserId { get; set; }
}
