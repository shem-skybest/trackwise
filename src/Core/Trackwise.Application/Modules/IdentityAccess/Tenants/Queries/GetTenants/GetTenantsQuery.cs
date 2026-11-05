using System.Text.Json.Serialization;
using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants.Queries;

public sealed class GetTenantsQuery : SearchableQuery, IQuery<PagedResult<TenantDto>>
{
    [JsonIgnore] public bool IncludeConnectionStrings { get; set; }
}
