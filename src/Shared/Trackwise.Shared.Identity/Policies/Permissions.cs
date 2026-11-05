using System.Reflection;

namespace Trackwise.Shared.Identity.Policies;

public static class Permission
{
    private static readonly Dictionary<string, IEnumerable<string>> Modules;

    static Permission()
    {
        Modules = typeof(Permission)
            .GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .Where(type => type is { IsClass: true, IsSealed: true, IsAbstract: true })
            .ToDictionary(
                type => type.Name,
                type => type.GetFields(BindingFlags.Public | BindingFlags.Static)
                    .Where(field => field is { IsLiteral: true, IsInitOnly: false })
                    .Select(field => (string)field.GetValue(null)!)
            );
    }

    public static class AppRole
    {
        public const string View = $"{nameof(Permission)}.{nameof(AppRole)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(AppRole)}.Manage";
    }

    public static class Employee
    {
        public const string View = $"{nameof(Permission)}.{nameof(Employee)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Employee)}.Manage";
    }

    public static class Customer
    {
        public const string View = $"{nameof(Permission)}.{nameof(Customer)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Customer)}.Manage";
    }

    public static class Driver
    {
        public const string View = $"{nameof(Permission)}.{nameof(Driver)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Driver)}.Manage";
    }

    public static class Notification
    {
        public const string View = $"{nameof(Permission)}.{nameof(Notification)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Notification)}.Manage";
    }

    public static class Load
    {
        public const string View = $"{nameof(Permission)}.{nameof(Load)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Load)}.Manage";
    }

    public static class Stat
    {
        public const string View = $"{nameof(Permission)}.{nameof(Stat)}.View";
    }

    public static class Tenant
    {
        public const string View = $"{nameof(Permission)}.{nameof(Tenant)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Tenant)}.Manage";
    }

    public static class TenantRole
    {
        public const string View = $"{nameof(Permission)}.{nameof(TenantRole)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(TenantRole)}.Manage";
    }

    public static class Truck
    {
        public const string View = $"{nameof(Permission)}.{nameof(Truck)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Truck)}.Manage";
    }

    public static class User
    {
        public const string View = $"{nameof(Permission)}.{nameof(User)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(User)}.Manage";
    }

    public static class Payment
    {
        public const string View = $"{nameof(Permission)}.{nameof(Payment)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Payment)}.Manage";
    }

    public static class Invoice
    {
        public const string View = $"{nameof(Permission)}.{nameof(Invoice)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Invoice)}.Manage";
        public const string Approve = $"{nameof(Permission)}.{nameof(Invoice)}.Approve";
    }

    public static class Tax
    {
        public const string View = $"{nameof(Permission)}.{nameof(Tax)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Tax)}.Manage";
    }

    public static class Payroll
    {
        public const string View = $"{nameof(Permission)}.{nameof(Payroll)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Payroll)}.Manage";
        public const string Approve = $"{nameof(Permission)}.{nameof(Payroll)}.Approve";
    }

    public static class Eld
    {
        public const string View = $"{nameof(Permission)}.{nameof(Eld)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Eld)}.Manage";
        public const string Sync = $"{nameof(Permission)}.{nameof(Eld)}.Sync";
    }

    public static class Accounting
    {
        public const string View = $"{nameof(Permission)}.{nameof(Accounting)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Accounting)}.Manage";
    }

    public static class Message
    {
        public const string View = $"{nameof(Permission)}.{nameof(Message)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Message)}.Manage";
    }

    public static class Invitation
    {
        public const string View = $"{nameof(Permission)}.{nameof(Invitation)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Invitation)}.Manage";
    }

    public static class Portal
    {
        public const string Access = $"{nameof(Permission)}.{nameof(Portal)}.Access";
        public const string ViewLoads = $"{nameof(Permission)}.{nameof(Portal)}.ViewLoads";
        public const string ViewInvoices = $"{nameof(Permission)}.{nameof(Portal)}.ViewInvoices";
        public const string ViewDocuments = $"{nameof(Permission)}.{nameof(Portal)}.ViewDocuments";
    }

    public static class Expense
    {
        public const string View = $"{nameof(Permission)}.{nameof(Expense)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Expense)}.Manage";
        public const string Approve = $"{nameof(Permission)}.{nameof(Expense)}.Approve";
    }

    public static class LoadBoard
    {
        public const string View = $"{nameof(Permission)}.{nameof(LoadBoard)}.View";
        public const string Search = $"{nameof(Permission)}.{nameof(LoadBoard)}.Search";
        public const string Book = $"{nameof(Permission)}.{nameof(LoadBoard)}.Book";
        public const string Post = $"{nameof(Permission)}.{nameof(LoadBoard)}.Post";
        public const string Manage = $"{nameof(Permission)}.{nameof(LoadBoard)}.Manage";
    }

    public static class FuelCard
    {
        public const string View = $"{nameof(Permission)}.{nameof(FuelCard)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(FuelCard)}.Manage";
    }

    public static class Dispatch
    {
        public const string View = $"{nameof(Permission)}.{nameof(Dispatch)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Dispatch)}.Manage";
    }

    public static class BlogPost
    {
        public const string View = $"{nameof(Permission)}.{nameof(BlogPost)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(BlogPost)}.Manage";
    }

    public static class IftaRate
    {
        public const string View = $"{nameof(Permission)}.{nameof(IftaRate)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(IftaRate)}.Manage";
    }

    public static class Dvir
    {
        public const string View = $"{nameof(Permission)}.{nameof(Dvir)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Dvir)}.Manage";
        public const string Review = $"{nameof(Permission)}.{nameof(Dvir)}.Review";
    }

    public static class Safety
    {
        public const string View = $"{nameof(Permission)}.{nameof(Safety)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Safety)}.Manage";
    }

    public static class Maintenance
    {
        public const string View = $"{nameof(Permission)}.{nameof(Maintenance)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Maintenance)}.Manage";
    }

    public static class ApiKey
    {
        public const string View = $"{nameof(Permission)}.{nameof(ApiKey)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(ApiKey)}.Manage";
    }

    public static class Container
    {
        public const string View = $"{nameof(Permission)}.{nameof(Container)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Container)}.Manage";
    }

    public static class Terminal
    {
        public const string View = $"{nameof(Permission)}.{nameof(Terminal)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Terminal)}.Manage";
    }

    public static class Copilot
    {
        public const string View = $"{nameof(Permission)}.{nameof(Copilot)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Copilot)}.Manage";
    }

    public static class Negotiation
    {
        public const string View = $"{nameof(Permission)}.{nameof(Negotiation)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Negotiation)}.Manage";
    }

    public static IEnumerable<string> GetAll()
    {
        var list = new List<string>();

        foreach (var module in Modules.Keys)
        {
            list.AddRange(GeneratePermissions(module));
        }
        return list;
    }

    // Forwarding Permissions
    public static class Quotation
    {
        public const string View = $"{nameof(Permission)}.{nameof(Quotation)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Quotation)}.Manage";
    }

    public static class Booking
    {
        public const string View = $"{nameof(Permission)}.{nameof(Booking)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Booking)}.Manage";

        /// <summary>Buying cost, margin and the internal PDF copy.</summary>
        public const string ViewCosts = $"{nameof(Permission)}.{nameof(Booking)}.ViewCosts";

        public const string ApproveCredit = $"{nameof(Permission)}.{nameof(Booking)}.ApproveCredit";
    }

    public static class ChargeCode
    {
        public const string View = $"{nameof(Permission)}.{nameof(ChargeCode)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(ChargeCode)}.Manage";
    }

    public static class Tariff
    {
        public const string View = $"{nameof(Permission)}.{nameof(Tariff)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Tariff)}.Manage";

        public static object Create(object mode, object serviceLevel, string name, string currency, string originLane, string destinationLane, object equipmentType, object carrierId, DateOnly effectiveFrom, object contractId)
        {
            throw new NotImplementedException();
        }
    }

    public static class Shipment
    {
        public const string View = $"{nameof(Permission)}.{nameof(Shipment)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Shipment)}.Manage";
    }

    public static class Compliance
    {
        public const string View = $"{nameof(Permission)}.{nameof(Compliance)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Compliance)}.Manage";
    }

    public static class Billing
    {
        public const string View = $"{nameof(Permission)}.{nameof(Billing)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Billing)}.Manage";
    }

    public static class BusinessPartner
    {
        public const string View = $"{nameof(Permission)}.{nameof(BusinessPartner)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(BusinessPartner)}.Manage";
    }

    /// <summary>
    /// Workfile carries verbs beyond View/Manage because the file changes hands: ops edits it,
    /// a manager and an approver sign it off, and Accounting alone releases a closed reference.
    /// Grant these individually per role - <see cref="GeneratePermissions"/> hands over all five.
    /// </summary>
    public static class Workfile
    {
        public const string View = $"{nameof(Permission)}.{nameof(Workfile)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(Workfile)}.Manage";
        public const string Approve = $"{nameof(Permission)}.{nameof(Workfile)}.Approve";
        public const string ForwardBilling = $"{nameof(Permission)}.{nameof(Workfile)}.ForwardBilling";
        public const string ClearClosure = $"{nameof(Permission)}.{nameof(Workfile)}.ClearClosure";
    }

    /// <summary>
    /// Governs every master-data entity group, not one entity: a steward who maintains the
    /// reference lists maintains all of them. Per-group steward scoping arrives with the entity
    /// groups themselves, once there is more than one group to scope.
    /// <para>
    /// <see cref="Approve"/> is a third verb rather than part of Manage, because the two are held
    /// by different people on purpose. Granting it alongside Manage - which Owner and Manager both
    /// get - is not the hole it looks like: the refusal is that the approver may not be the
    /// <i>submitter of that record</i>, checked per submission, so a role that can do both still
    /// cannot do both to the same record.
    /// </para>
    /// </summary>
    public static class MasterData
    {
        public const string View = $"{nameof(Permission)}.{nameof(MasterData)}.View";
        public const string Manage = $"{nameof(Permission)}.{nameof(MasterData)}.Manage";
        public const string Approve = $"{nameof(Permission)}.{nameof(MasterData)}.Approve";
    }

    public static IEnumerable<string> GeneratePermissions(string module)
    {
        return Modules[module];
    }
}
