namespace Trackwise.Shared.Identity.Roles;

/// <summary>
/// The built-in tenant roles, one per job. The first five keys predate the job-based roles and
/// are matched by name across the code (Telegram, stats, the driver crew list), so they keep their
/// keys and only their display names follow the job: Manager is the Ops Manager, Dispatcher the
/// combined CS + Dispatch desk that new staff start in.
/// </summary>
public static class TenantRoles
{
    public const string Owner = "tenant.owner";
    public const string Manager = "tenant.manager";
    public const string Dispatcher = "tenant.dispatcher";
    public const string Driver = "tenant.driver";
    public const string Customer = "tenant.customer";

    public const string Executive = "tenant.executive";
    public const string SystemAdmin = "tenant.system_admin";
    public const string ColdChainManager = "tenant.cold_chain_manager";
    public const string ForwardingSupervisor = "tenant.forwarding_supervisor";
    public const string FleetSupervisor = "tenant.fleet_supervisor";
    public const string LiaisonOfficer = "tenant.liaison_officer";
    public const string Marketing = "tenant.marketing";
    public const string MarketingManager = "tenant.marketing_manager";
    public const string Accounting = "tenant.accounting";
    public const string AccountingManager = "tenant.accounting_manager";
    public const string HumanResources = "tenant.hr";
    public const string AdminStaff = "tenant.admin_staff";

    public static IEnumerable<EnumType<string>> GetValues()
    {
        yield return new EnumType<string>(Owner, "Owner");
        yield return new EnumType<string>(Executive, "Executive");
        yield return new EnumType<string>(SystemAdmin, "System Admin (IT)");
        yield return new EnumType<string>(Manager, "Ops Manager");
        yield return new EnumType<string>(ColdChainManager, "Cold Chain Manager");
        yield return new EnumType<string>(ForwardingSupervisor, "Forwarding Supervisor");
        yield return new EnumType<string>(Dispatcher, "CS + Dispatch");
        yield return new EnumType<string>(LiaisonOfficer, "Liaison Officer");
        yield return new EnumType<string>(FleetSupervisor, "Fleet Supervisor");
        yield return new EnumType<string>(Marketing, "Marketing");
        yield return new EnumType<string>(MarketingManager, "Marketing Manager");
        yield return new EnumType<string>(Accounting, "Accounting");
        yield return new EnumType<string>(AccountingManager, "Accounting Manager");
        yield return new EnumType<string>(HumanResources, "HR");
        yield return new EnumType<string>(AdminStaff, "Admin Staff");
        yield return new EnumType<string>(Driver, "Driver / Helper");
        yield return new EnumType<string>(Customer, "Customer");
    }

    public static bool IsBuiltIn(string roleName) => GetValues().Any(r => r.Value == roleName);

    public static string? GetDisplayName(string roleName) =>
        GetValues().FirstOrDefault(r => r.Value == roleName)?.DisplayName;
}
