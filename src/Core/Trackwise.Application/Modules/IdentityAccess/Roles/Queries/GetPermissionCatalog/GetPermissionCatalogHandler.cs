using Trackwise.Application.Abstractions;
using Trackwise.Shared.Identity.Policies;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Queries;

internal sealed class GetPermissionCatalogHandler : IAppRequestHandler<GetPermissionCatalogQuery, Result<PermissionGroupDto[]>>
{
    public Task<Result<PermissionGroupDto[]>> Handle(GetPermissionCatalogQuery req, CancellationToken ct)
    {
        var basic = TenantRolePermissions.GetBasicPermissions().ToHashSet();

        // "Permission.{Module}.{Verb}" - the shape every constant in Permissions.cs follows.
        var groups = TenantRolePermissions.Catalog
            .Select(p => (Name: p, Parts: p.Split('.')))
            .Where(p => p.Parts.Length == 3)
            .GroupBy(p => p.Parts[1])
            .Select(g =>
            {
                var (label, section) = PermissionCatalogLabels.Module(g.Key);
                return new PermissionGroupDto
                {
                    Module = g.Key,
                    Label = label,
                    Section = section,
                    Permissions = g.Select(p => new PermissionOptionDto
                    {
                        Name = p.Name,
                        Label = PermissionCatalogLabels.Verb(p.Parts[2]),
                        IsBasic = basic.Contains(p.Name)
                    }).ToArray()
                };
            })
            .OrderBy(g => Array.IndexOf(PermissionCatalogLabels.Sections, g.Section))
            .ThenBy(g => g.Label)
            .ToArray();

        return Task.FromResult(Result<PermissionGroupDto[]>.Ok(groups));
    }
}
