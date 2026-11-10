using System.Text.RegularExpressions;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Roles.Commands;

internal sealed partial class CreateTenantRoleHandler(
    ITenantUnitOfWork tenantUow,
    ITenantRoleAccess roleAccess,
    ITenantRolePermissionWriter writer)
    : IAppRequestHandler<CreateTenantRoleCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateTenantRoleCommand req, CancellationToken ct)
    {
        var roles = tenantUow.Repository<TenantRole>();
        var displayName = req.DisplayName.Trim();

        var source = await roles.GetAsync(r => r.Name == req.CopyFromRoleName, ct);
        if (source is null)
        {
            return Result<string>.Fail($"Could not find role '{req.CopyFromRoleName}'");
        }

        if (source.Name == TenantRoles.Customer)
        {
            return Result<string>.Fail("The Customer role can't be copied into a staff role.");
        }

        var lowered = displayName.ToLower();
        if (await roles.GetAsync(r => r.DisplayName != null && r.DisplayName.ToLower() == lowered, ct) is not null)
        {
            return Result<string>.Fail($"A role named '{displayName}' already exists.");
        }

        var role = new TenantRole(await UniqueNameAsync(displayName, ct))
        {
            DisplayName = displayName,
            IsCustomized = true
        };
        await roles.AddAsync(role, ct);

        var sourcePermissions = await roleAccess.GetRolePermissionsAsync(source.Id, ct);
        var result = await writer.ReplaceAsync(role, sourcePermissions, ct);
        if (!result.IsSuccess)
        {
            return Result<string>.Fail(result.Error!);
        }

        await writer.LogAsync(role, TenantRoleChangeType.RoleCreated, null, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<string>.Ok(role.Name);
    }

    private async Task<string> UniqueNameAsync(string displayName, CancellationToken ct)
    {
        var slug = NonAlphanumeric().Replace(displayName.ToLowerInvariant(), "_").Trim('_');
        var baseName = $"tenant.custom_{(slug.Length == 0 ? "role" : slug)}";
        var name = baseName;

        for (var i = 2; await tenantUow.Repository<TenantRole>().GetAsync(r => r.Name == name, ct) is not null; i++)
        {
            name = $"{baseName}_{i}";
        }

        return name;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumeric();
}
