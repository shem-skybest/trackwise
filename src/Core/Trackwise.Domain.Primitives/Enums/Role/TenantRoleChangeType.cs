namespace Trackwise.Domain.Primitives.Enums;

/// <summary>What a <c>TenantRoleChangeLog</c> row records.</summary>
public enum TenantRoleChangeType
{
    RoleCreated = 0,
    RoleDeleted = 1,
    PermissionAdded = 2,
    PermissionRemoved = 3,

    /// <summary>The role was put back to its built-in permission bundle.</summary>
    ResetToDefaults = 4,

    /// <summary>A person was moved to this role from another (named in <c>PreviousRoleName</c>).</summary>
    EmployeeAssigned = 5
}
