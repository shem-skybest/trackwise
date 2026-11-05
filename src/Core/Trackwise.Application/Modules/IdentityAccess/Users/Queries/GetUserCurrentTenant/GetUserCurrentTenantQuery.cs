using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Users.Queries;

public class GetUserCurrentTenantQuery : IQuery<Result<TenantDto>>
{
    public Guid UserId { get; set; }
}
