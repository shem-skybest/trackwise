using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;
using Riok.Mapperly.Abstractions;

namespace Trackwise.Mappings;

[Mapper]
public static partial class InvitationMapper
{
    /// <summary>
    /// Maps an Invitation entity to InvitationDto with tenant information.
    /// </summary>
    [UserMapping(Default = true)]
    public static InvitationDto ToDto(this Invitation entity)
    {
        return new InvitationDto
        {
            Id = entity.Id,
            Email = entity.Email,
            TenantId = entity.TenantId,
            TenantName = entity.Tenant?.CompanyName ?? entity.Tenant?.Name ?? string.Empty,
            Type = entity.Type,
            TenantRole = entity.TenantRole,
            TenantRoleDisplayName = entity.Type == InvitationType.AppUser
                ? GetAppRoleDisplayName(entity.AppRole)
                : GetRoleDisplayName(entity.TenantRole),
            AppRole = entity.AppRole,
            CustomerId = entity.CustomerId,
            CustomerName = null, // Set by handler with tenant DB lookup
            ExpiresAt = entity.ExpiresAt,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            InvitedByName = entity.InvitedByUser?.GetFullName() ?? string.Empty,
            SendCount = entity.SendCount,
            LastSentAt = entity.LastSentAt,
            IsExpired = entity.IsExpired
        };
    }

    /// <summary>
    /// Maps an Invitation entity to InvitationDto with explicit tenant and inviter names.
    /// </summary>
    public static InvitationDto ToDto(this Invitation entity, string tenantName, string invitedByName, string? customerName = null)
    {
        var dto = entity.ToDto();
        return dto with
        {
            TenantName = tenantName,
            InvitedByName = invitedByName,
            CustomerName = customerName
        };
    }

    /// <summary>
    /// Gets the display name for a tenant role.
    /// </summary>
    public static string GetRoleDisplayName(string? role) =>
        role is null ? string.Empty : TenantRoles.GetDisplayName(role) ?? role;

    /// <summary>
    /// Gets the display name for an app-level role, sourced from <see cref="AppRoles.GetValues"/>.
    /// </summary>
    public static string GetAppRoleDisplayName(string? role) =>
        AppRoles.GetValues().FirstOrDefault(v => v.Value == role)?.DisplayName ?? role ?? string.Empty;
}
