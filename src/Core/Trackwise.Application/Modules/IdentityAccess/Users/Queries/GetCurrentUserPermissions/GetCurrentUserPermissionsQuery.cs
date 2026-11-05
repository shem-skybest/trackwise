using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Queries;

public sealed class GetCurrentUserPermissionsQuery : IQuery<Result<string[]>>
{
    public required Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
}
