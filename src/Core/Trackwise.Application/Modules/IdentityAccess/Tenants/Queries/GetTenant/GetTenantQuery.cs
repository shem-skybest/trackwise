using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants.Queries;

public sealed class GetTenantQuery : IQuery<Result<TenantDto>>
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public bool IncludeConnectionString { get; set; }
}
