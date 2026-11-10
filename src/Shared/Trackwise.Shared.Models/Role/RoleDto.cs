namespace Trackwise.Shared.Models;

public class RoleDto
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }

    /// <summary>Tenant roles only: shipped with the app rather than created in Settings.</summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>Tenant roles only: someone changed its permissions, so the migrator leaves it alone.</summary>
    public bool IsCustomized { get; set; }

    public int EmployeeCount { get; set; }
    public int PermissionCount { get; set; }

    /// <summary>Whether the signed-in user may change this role's permissions.</summary>
    public bool CanEdit { get; set; }

    /// <summary>Whether the signed-in user may put someone in this role.</summary>
    public bool CanAssign { get; set; }
}
