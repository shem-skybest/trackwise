namespace Trackwise.Application.Modules.IdentityAccess.Roles;

/// <summary>
/// The words the role editor shows for each permission module and verb. A module missing here
/// still appears, under "Other" with its code name, so a new permission is never hidden from the
/// Owner - it just reads less nicely until it gets a label.
/// </summary>
internal static class PermissionCatalogLabels
{
    public const string OtherSection = "Other";

    /// <summary>Section order in the editor.</summary>
    public static readonly string[] Sections =
        ["Operations", "Forwarding", "Fleet", "Sales", "Money", "People & system", OtherSection, "US-only (not used in PH)"];

    private static readonly Dictionary<string, (string Label, string Section)> Modules = new()
    {
        ["Load"] = ("Loads, trips & crew", "Operations"),
        ["Dispatch"] = ("Dispatch board & AI dispatch", "Operations"),
        ["Safety"] = ("Incidents & rescues", "Operations"),
        ["Customer"] = ("Customers", "Operations"),
        ["Copilot"] = ("AI copilot", "Operations"),
        ["Notification"] = ("Notifications", "Operations"),
        ["Message"] = ("Messages", "Operations"),
        ["Stat"] = ("Dashboard & reports", "Operations"),
        ["Booking"] = ("Bookings", "Forwarding"),
        ["Workfile"] = ("Workfiles", "Forwarding"),
        ["Container"] = ("Containers", "Forwarding"),
        ["Terminal"] = ("Terminals & ports", "Forwarding"),
        ["Shipment"] = ("Shipments", "Forwarding"),
        ["Compliance"] = ("Shipment documents & compliance", "Forwarding"),
        ["Truck"] = ("Trucks & vehicle papers", "Fleet"),
        ["Maintenance"] = ("Repairs & PMS", "Fleet"),
        ["Dvir"] = ("Truck checks (DVIR)", "Fleet"),
        ["Driver"] = ("Drivers", "Fleet"),
        ["Quotation"] = ("Quotations", "Sales"),
        ["Tariff"] = ("Tariffs, rate agreements & diesel price", "Sales"),
        ["Negotiation"] = ("Rate negotiation & floors", "Sales"),
        ["BusinessPartner"] = ("Business partners", "Sales"),
        ["Invoice"] = ("Invoices & billing batches", "Money"),
        ["Billing"] = ("Billing", "Money"),
        ["Payment"] = ("Payments", "Money"),
        ["Expense"] = ("Expenses, cash funds & fuel log", "Money"),
        ["ChargeCode"] = ("Charge codes", "Money"),
        ["Accounting"] = ("Accounting sync", "Money"),
        ["Payroll"] = ("Payroll & timesheets", "Money"),
        ["Tax"] = ("Tax settings", "Money"),
        ["Employee"] = ("Employees", "People & system"),
        ["Invitation"] = ("Invitations", "People & system"),
        ["TenantRole"] = ("Roles & permissions", "People & system"),
        ["Tenant"] = ("Company settings", "People & system"),
        ["ApiKey"] = ("API keys", "People & system"),
        ["MasterData"] = ("Master data", "People & system"),
        ["Eld"] = ("ELD / hours of service", "US-only (not used in PH)"),
        ["LoadBoard"] = ("Load boards", "US-only (not used in PH)"),
        ["FuelCard"] = ("Fuel cards", "US-only (not used in PH)"),
    };

    private static readonly Dictionary<string, string> Verbs = new()
    {
        ["View"] = "View",
        ["Manage"] = "Create & edit",
        ["Approve"] = "Approve",
        ["Review"] = "Review",
        ["Sync"] = "Sync",
        ["Search"] = "Search",
        ["Book"] = "Book",
        ["Post"] = "Post",
        ["ViewCosts"] = "See buying costs & margin",
        ["ApproveCredit"] = "Approve credit",
        ["ForwardBilling"] = "Forward to billing",
        ["ClearClosure"] = "Release closed workfile",
    };

    public static (string Label, string Section) Module(string module) =>
        Modules.TryGetValue(module, out var entry) ? entry : (module, OtherSection);

    public static string Verb(string verb) => Verbs.GetValueOrDefault(verb, verb);
}
