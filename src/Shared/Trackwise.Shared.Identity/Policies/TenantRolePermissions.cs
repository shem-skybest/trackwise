using Trackwise.Shared.Identity.Roles;

namespace Trackwise.Shared.Identity.Policies;

/// <summary>
/// The default permission bundle of each built-in tenant role. These are defaults only: once an
/// Owner or System Admin edits a role in Settings, the role is marked customized and the migrator
/// stops re-syncing it from here (Owner excepted - it always holds the whole catalog).
/// </summary>
public static class TenantRolePermissions
{
    private static readonly Dictionary<string, Func<IEnumerable<string>>> RolePermissions = new()
    {
        [TenantRoles.Owner] = GetOwnerPermissions,
        [TenantRoles.Executive] = GetExecutivePermissions,
        [TenantRoles.SystemAdmin] = GetSystemAdminPermissions,
        [TenantRoles.Manager] = GetOpsManagerPermissions,
        [TenantRoles.ColdChainManager] = GetColdChainManagerPermissions,
        [TenantRoles.ForwardingSupervisor] = GetForwardingSupervisorPermissions,
        [TenantRoles.Dispatcher] = GetCsDispatchPermissions,
        [TenantRoles.LiaisonOfficer] = GetLiaisonOfficerPermissions,
        [TenantRoles.FleetSupervisor] = GetFleetSupervisorPermissions,
        [TenantRoles.Marketing] = GetMarketingPermissions,
        [TenantRoles.MarketingManager] = GetMarketingManagerPermissions,
        [TenantRoles.Accounting] = GetAccountingPermissions,
        [TenantRoles.AccountingManager] = GetAccountingManagerPermissions,
        [TenantRoles.HumanResources] = GetHrPermissions,
        [TenantRoles.AdminStaff] = GetAdminStaffPermissions,
        [TenantRoles.Driver] = GetDriverPermissions,
        [TenantRoles.Customer] = GetCustomerPermissions,
    };

    public static IEnumerable<string> Owner => GetOwnerPermissions();
    public static IEnumerable<string> Manager => GetOpsManagerPermissions();
    public static IEnumerable<string> Dispatcher => GetCsDispatchPermissions();
    public static IEnumerable<string> Driver => GetDriverPermissions();
    public static IEnumerable<string> Customer => GetCustomerPermissions();

    /// <summary>
    /// Every permission a tenant role may hold - what the role editor offers. It is the Owner's
    /// bundle: anything outside it (AppRole, User, BlogPost, IftaRate, Portal) belongs to the
    /// platform admins or the customer portal, not to tenant staff.
    /// </summary>
    public static IReadOnlyList<string> Catalog => GetOwnerPermissions().Distinct().ToList();

    /// <summary>
    /// Gets permissions for a specific role by name.
    /// Returns empty if role is not found.
    /// </summary>
    public static IEnumerable<string> GetPermissionsForRole(string roleName)
    {
        return RolePermissions.TryGetValue(roleName, out var permissionsFunc)
            ? permissionsFunc()
            : [];
    }

    /// <summary>The full default bundle of a built-in role: basic permissions plus its own, de-duplicated.</summary>
    public static IReadOnlyList<string> GetDefaultsForRole(string roleName) =>
        GetBasicPermissions().Concat(GetPermissionsForRole(roleName)).Distinct().ToList();

    /// <summary>
    /// Registers permissions for a role. Use this to add new roles dynamically.
    /// </summary>
    public static void RegisterRole(string roleName, Func<IEnumerable<string>> permissionsFunc)
    {
        RolePermissions[roleName] = permissionsFunc;
    }

    /// <summary>
    /// Checks if a role has registered permissions.
    /// </summary>
    public static bool HasRegisteredPermissions(string roleName)
    {
        return RolePermissions.ContainsKey(roleName);
    }

    public static IEnumerable<string> GetBasicPermissions() => AppRolePermissions.GetBasicPermissions();

    private static IEnumerable<string> All(string module) => Permission.GeneratePermissions(module);

    /// <summary>Every office role gets these: notifications, messages and the dashboard numbers.</summary>
    private static List<string> StaffBase()
    {
        var list = new List<string>();
        list.AddRange(GetBasicPermissions());
        list.AddRange(All(nameof(Permission.Notification)));
        list.AddRange(All(nameof(Permission.Message)));
        list.Add(Permission.Stat.View);
        list.AddRange(All(nameof(Permission.Copilot)));
        return list;
    }

    private static IEnumerable<string> GetOwnerPermissions()
    {
        var list = new List<string>();
        list.AddRange(GetBasicPermissions());
        list.AddRange(All(nameof(Permission.Employee)));
        list.AddRange(All(nameof(Permission.Driver)));
        list.AddRange(All(nameof(Permission.Load)));
        list.AddRange(All(nameof(Permission.Truck)));
        list.AddRange(All(nameof(Permission.TenantRole)));
        list.AddRange(All(nameof(Permission.Notification)));
        list.AddRange(All(nameof(Permission.Stat)));
        list.AddRange(All(nameof(Permission.Customer)));
        list.AddRange(All(nameof(Permission.Payment)));
        list.AddRange(All(nameof(Permission.Invoice)));
        list.AddRange(All(nameof(Permission.Tax)));
        list.AddRange(All(nameof(Permission.Payroll)));
        list.AddRange(All(nameof(Permission.Eld)));
        list.AddRange(All(nameof(Permission.Accounting)));
        list.AddRange(All(nameof(Permission.Message)));
        list.AddRange(All(nameof(Permission.Invitation)));
        list.AddRange(All(nameof(Permission.Expense)));
        list.AddRange(All(nameof(Permission.LoadBoard)));
        list.AddRange(All(nameof(Permission.FuelCard)));
        list.AddRange(All(nameof(Permission.Tenant)));
        list.AddRange(All(nameof(Permission.Dvir)));
        list.AddRange(All(nameof(Permission.Safety)));
        list.AddRange(All(nameof(Permission.Maintenance)));
        list.AddRange(All(nameof(Permission.Dispatch)));
        list.AddRange(All(nameof(Permission.ApiKey)));
        list.AddRange(All(nameof(Permission.Container)));
        list.AddRange(All(nameof(Permission.Terminal)));
        list.AddRange(All(nameof(Permission.Copilot)));
        list.AddRange(All(nameof(Permission.Negotiation)));
        list.AddRange(All(nameof(Permission.Booking)));
        list.AddRange(All(nameof(Permission.Tariff)));
        list.AddRange(All(nameof(Permission.Shipment)));
        list.AddRange(All(nameof(Permission.Quotation)));
        list.AddRange(All(nameof(Permission.Compliance)));
        list.AddRange(All(nameof(Permission.ChargeCode)));
        list.AddRange(All(nameof(Permission.Billing)));
        list.AddRange(All(nameof(Permission.BusinessPartner)));
        list.AddRange(All(nameof(Permission.MasterData)));
        list.AddRange(All(nameof(Permission.Workfile)));
        return list;
    }

    /// <summary>The GM: sees and approves everything, but cannot change who may do what.</summary>
    private static IEnumerable<string> GetExecutivePermissions() =>
        GetOwnerPermissions().Where(p => p is not (Permission.TenantRole.Manage or Permission.ApiKey.Manage));

    /// <summary>
    /// IT runs the accounts and the system, not the operation: users, roles, settings and keys,
    /// with read-only operations so they can support people. Because nobody may grant a permission
    /// they don't hold, IT can only hand out what is in this bundle - ops grants stay with the Owner.
    /// </summary>
    private static IEnumerable<string> GetSystemAdminPermissions()
    {
        var list = StaffBase();
        list.AddRange(All(nameof(Permission.Employee)));
        list.AddRange(All(nameof(Permission.Invitation)));
        list.AddRange(All(nameof(Permission.TenantRole)));
        list.AddRange(All(nameof(Permission.Tenant)));
        list.AddRange(All(nameof(Permission.ApiKey)));
        list.Add(Permission.MasterData.View);
        list.Add(Permission.Load.View);
        list.Add(Permission.Booking.View);
        list.Add(Permission.Quotation.View);
        list.Add(Permission.Workfile.View);
        list.Add(Permission.Customer.View);
        list.Add(Permission.Truck.View);
        list.Add(Permission.BusinessPartner.View);
        return list;
    }

    /// <summary>Line and branch ops heads: every operations screen, approvals and costs; money read-only.</summary>
    private static List<string> GetOpsManagerPermissions()
    {
        var list = StaffBase();
        list.AddRange(All(nameof(Permission.Load)));
        list.AddRange(All(nameof(Permission.Dispatch)));
        list.AddRange(All(nameof(Permission.Container)));
        list.AddRange(All(nameof(Permission.Terminal)));
        list.AddRange(All(nameof(Permission.Safety)));
        list.AddRange(All(nameof(Permission.Dvir)));
        list.AddRange(All(nameof(Permission.Customer)));
        list.AddRange(All(nameof(Permission.Booking)));
        list.AddRange(All(nameof(Permission.Shipment)));
        list.AddRange(All(nameof(Permission.Compliance)));
        list.AddRange(All(nameof(Permission.Expense)));
        list.AddRange(All(nameof(Permission.Invitation)));
        list.AddRange(All(nameof(Permission.Negotiation)));
        list.Add(Permission.Workfile.View);
        list.Add(Permission.Workfile.Manage);
        list.Add(Permission.Workfile.Approve);
        list.Add(Permission.Workfile.ForwardBilling);
        list.Add(Permission.Quotation.View);
        list.Add(Permission.Tariff.View);
        list.Add(Permission.Truck.View);
        list.Add(Permission.Driver.View);
        list.Add(Permission.Maintenance.View);
        list.Add(Permission.BusinessPartner.View);
        list.Add(Permission.MasterData.View);
        list.Add(Permission.MasterData.Manage);
        list.Add(Permission.Invoice.View);
        list.Add(Permission.Payment.View);
        list.Add(Permission.Employee.Manage);
        return list;
    }

    /// <summary>Ops Manager plus the CCL-specific levers: rate agreements, diesel price and the fleet.</summary>
    private static IEnumerable<string> GetColdChainManagerPermissions()
    {
        var list = GetOpsManagerPermissions();
        list.AddRange(All(nameof(Permission.Tariff)));
        list.AddRange(All(nameof(Permission.Truck)));
        list.AddRange(All(nameof(Permission.Maintenance)));
        return list;
    }

    private static IEnumerable<string> GetForwardingSupervisorPermissions()
    {
        var list = StaffBase();
        list.AddRange(All(nameof(Permission.Booking)));
        list.AddRange(All(nameof(Permission.Container)));
        list.AddRange(All(nameof(Permission.Terminal)));
        list.AddRange(All(nameof(Permission.Shipment)));
        list.AddRange(All(nameof(Permission.Compliance)));
        list.AddRange(All(nameof(Permission.Customer)));
        list.Add(Permission.Workfile.View);
        list.Add(Permission.Workfile.Manage);
        list.Add(Permission.Workfile.Approve);
        list.Add(Permission.Workfile.ForwardBilling);
        list.Add(Permission.Quotation.View);
        list.Add(Permission.Tariff.View);
        list.Add(Permission.Load.View);
        list.Add(Permission.Dispatch.View);
        list.Add(Permission.BusinessPartner.View);
        list.Add(Permission.MasterData.View);
        list.Add(Permission.Expense.View);
        list.Add(Permission.Expense.Manage);
        list.Add(Permission.Invoice.View);
        return list;
    }

    /// <summary>
    /// The combined desk: takes the booking and dispatches it. No buying costs, no credit approval,
    /// no closing workfiles. New and untitled staff start here until HR assigns them.
    /// </summary>
    private static IEnumerable<string> GetCsDispatchPermissions()
    {
        var list = StaffBase();
        list.AddRange(All(nameof(Permission.Load)));
        list.AddRange(All(nameof(Permission.Dispatch)));
        list.AddRange(All(nameof(Permission.Container)));
        list.AddRange(All(nameof(Permission.Terminal)));
        list.AddRange(All(nameof(Permission.Customer)));
        // ops log incidents (breakdowns, backloads, accidents) and the rescues they call for
        list.AddRange(All(nameof(Permission.Safety)));
        list.Add(Permission.Booking.View);
        list.Add(Permission.Booking.Manage);
        list.Add(Permission.Workfile.View);
        list.Add(Permission.Workfile.Manage);
        list.Add(Permission.Shipment.View);
        list.Add(Permission.Shipment.Manage);
        list.Add(Permission.Quotation.View);
        list.Add(Permission.Tariff.View);
        list.Add(Permission.Truck.View);
        list.Add(Permission.Driver.View);
        list.Add(Permission.Dvir.View);
        list.Add(Permission.Maintenance.View);
        list.Add(Permission.Expense.View);
        list.Add(Permission.Expense.Manage);
        list.Add(Permission.BusinessPartner.View);
        list.Add(Permission.MasterData.View);
        list.Add(Permission.Invoice.View);
        return list;
    }

    /// <summary>Runs papers, pre-departure inspections and the fuel log; edits workfile documents.</summary>
    private static IEnumerable<string> GetLiaisonOfficerPermissions()
    {
        var list = StaffBase();
        list.Add(Permission.Workfile.View);
        list.Add(Permission.Workfile.Manage);
        list.Add(Permission.Shipment.View);
        list.Add(Permission.Shipment.Manage);
        list.Add(Permission.Compliance.View);
        list.Add(Permission.Compliance.Manage);
        list.Add(Permission.Booking.View);
        list.Add(Permission.Container.View);
        list.Add(Permission.Load.View);
        list.Add(Permission.Truck.View);
        list.Add(Permission.Dvir.View);
        list.Add(Permission.Dvir.Manage);
        list.Add(Permission.Expense.View);
        list.Add(Permission.Expense.Manage);
        list.Add(Permission.MasterData.View);
        return list;
    }

    /// <summary>Motorpool: trucks, permits, repairs/PMS, truck checks and the fuel log.</summary>
    private static IEnumerable<string> GetFleetSupervisorPermissions()
    {
        var list = StaffBase();
        list.AddRange(All(nameof(Permission.Truck)));
        list.AddRange(All(nameof(Permission.Maintenance)));
        list.AddRange(All(nameof(Permission.Dvir)));
        list.AddRange(All(nameof(Permission.Safety)));
        list.Add(Permission.Expense.View);
        list.Add(Permission.Expense.Manage);
        list.Add(Permission.Driver.View);
        list.Add(Permission.Load.View);
        list.Add(Permission.Dispatch.View);
        list.Add(Permission.MasterData.View);
        return list;
    }

    /// <summary>The whole team sees every customer and quotation - they work as one department.</summary>
    private static List<string> GetMarketingPermissions()
    {
        var list = StaffBase();
        list.AddRange(All(nameof(Permission.Customer)));
        list.AddRange(All(nameof(Permission.Quotation)));
        list.Add(Permission.Tariff.View);
        list.Add(Permission.Booking.View);
        list.Add(Permission.Workfile.View);
        list.Add(Permission.Load.View);
        list.Add(Permission.BusinessPartner.View);
        list.Add(Permission.MasterData.View);
        return list;
    }

    private static IEnumerable<string> GetMarketingManagerPermissions()
    {
        var list = GetMarketingPermissions();
        list.AddRange(All(nameof(Permission.Tariff)));
        list.Add(Permission.Booking.ViewCosts);
        return list;
    }

    private static List<string> GetAccountingPermissions()
    {
        var list = StaffBase();
        list.AddRange(All(nameof(Permission.Payment)));
        list.AddRange(All(nameof(Permission.Billing)));
        list.AddRange(All(nameof(Permission.ChargeCode)));
        list.AddRange(All(nameof(Permission.Accounting)));
        list.Add(Permission.Invoice.View);
        list.Add(Permission.Invoice.Manage);
        list.Add(Permission.Expense.View);
        list.Add(Permission.Expense.Manage);
        list.Add(Permission.Booking.View);
        list.Add(Permission.Booking.ViewCosts);
        list.Add(Permission.Workfile.View);
        list.Add(Permission.Quotation.View);
        list.Add(Permission.Tariff.View);
        list.Add(Permission.Customer.View);
        list.Add(Permission.Load.View);
        list.Add(Permission.BusinessPartner.View);
        list.Add(Permission.MasterData.View);
        list.Add(Permission.Tax.View);
        return list;
    }

    /// <summary>Accounting plus the sign-offs: invoices, expenses, credit, payroll and releasing closed workfiles.</summary>
    private static IEnumerable<string> GetAccountingManagerPermissions()
    {
        var list = GetAccountingPermissions();
        list.AddRange(All(nameof(Permission.Invoice)));
        list.AddRange(All(nameof(Permission.Expense)));
        list.AddRange(All(nameof(Permission.Payroll)));
        list.AddRange(All(nameof(Permission.Tax)));
        list.Add(Permission.Booking.ApproveCredit);
        list.Add(Permission.Workfile.ClearClosure);
        return list;
    }

    private static IEnumerable<string> GetHrPermissions()
    {
        var list = StaffBase();
        list.AddRange(All(nameof(Permission.Employee)));
        list.AddRange(All(nameof(Permission.Invitation)));
        list.Add(Permission.Driver.View);
        list.Add(Permission.Payroll.View);
        list.Add(Permission.MasterData.View);
        return list;
    }

    /// <summary>Office admin: permits, cash funds, insurance claims and supplier records.</summary>
    private static IEnumerable<string> GetAdminStaffPermissions()
    {
        var list = StaffBase();
        list.Add(Permission.Truck.View);
        list.Add(Permission.Truck.Manage);
        list.Add(Permission.Expense.View);
        list.Add(Permission.Expense.Manage);
        list.Add(Permission.Maintenance.View);
        list.Add(Permission.Safety.View);
        list.Add(Permission.Customer.View);
        list.Add(Permission.BusinessPartner.View);
        list.Add(Permission.MasterData.View);
        return list;
    }

    private static IEnumerable<string> GetDriverPermissions()
    {
        var list = new List<string>();
        list.AddRange(GetBasicPermissions());
        list.AddRange(All(nameof(Permission.Driver)));
        list.AddRange(All(nameof(Permission.Message)));
        list.Add(Permission.Truck.View);
        list.Add(Permission.Load.View);
        list.Add(Permission.Stat.View);
        // Drivers file their own DVIRs; Dvir.Review stays with the roles that supervise them.
        list.Add(Permission.Dvir.View);
        list.Add(Permission.Dvir.Manage);
        list.AddRange(All(nameof(Permission.Copilot)));
        return list;
    }

    private static IEnumerable<string> GetCustomerPermissions()
    {
        var list = new List<string>();
        list.AddRange(GetBasicPermissions());
        list.AddRange(All(nameof(Permission.Portal)));
        return list;
    }
}
