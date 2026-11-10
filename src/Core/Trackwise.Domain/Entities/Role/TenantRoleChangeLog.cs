using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Append-only record of who changed what about access: a role's permissions, a role created or
/// deleted, or a person moved between roles. Role and employee names are copied in, so the log stays
/// readable after a role is deleted or a person leaves.
/// <para>
/// The staff seeder also reads it: an employee with an <see cref="TenantRoleChangeType.EmployeeAssigned"/>
/// row has been placed by a person, and the seed roster no longer overrides their role.
/// </para>
/// </summary>
public class TenantRoleChangeLog : Entity, ITenantEntity
{
    public required TenantRoleChangeType ChangeType { get; set; }

    public required string RoleName { get; set; }

    public string? RoleDisplayName { get; set; }

    /// <summary>Set for <see cref="TenantRoleChangeType.PermissionAdded"/> and <see cref="TenantRoleChangeType.PermissionRemoved"/>.</summary>
    public string? Permission { get; set; }

    /// <summary>Set for <see cref="TenantRoleChangeType.EmployeeAssigned"/>.</summary>
    public Guid? EmployeeId { get; set; }

    public string? EmployeeName { get; set; }

    public string? PreviousRoleName { get; set; }

    public Guid? ChangedById { get; set; }

    public string? ChangedByName { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
