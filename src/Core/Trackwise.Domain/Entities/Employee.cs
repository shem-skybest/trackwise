using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A tenant employee - dispatcher, manager, owner, driver, or anyone else who works for the
/// company - as a canonical master-data record. The hardest promotion in the module: this entity
/// shares its primary key with the master-database <see cref="User" /> (by convention, set once in
/// <see cref="CreateEmployeeFromUser" />, not an EF-enforced relationship), and carries payroll,
/// Stripe Connect and invitation machinery no other master entity does - see <c>Helper</c>'s own doc
/// comment for why that entity was deliberately kept separate rather than built on this one.
/// </summary>
public class Employee : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    public MasterDataGroup Group => MasterDataGroup.FleetAndPersonnel;

    private string code = "";

    /// <summary>
    /// The stable handle other systems join on. There is no natural source for it the way a
    /// vessel has an IMO number or a trailer a VIN - it is always allocated from the
    /// <c>Employee</c> number series, the same shape as <c>Branch</c>'s code.
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Email { get; set; }

    private string firstName = "";
    private string lastName = "";

    public required string FirstName
    {
        get => firstName;
        set
        {
            firstName = value;
            RecomputeName();
        }
    }

    public required string LastName
    {
        get => lastName;
        set
        {
            lastName = value;
            RecomputeName();
        }
    }

    /// <summary>
    /// Backs the module's required <see cref="IMasterDataRecord.Name" /> so the duplicate probe's
    /// trigram index has a real column to match against. Kept in sync by the entity itself whenever
    /// <see cref="FirstName" /> or <see cref="LastName" /> changes, rather than asking every one of
    /// the ~105 call sites that already read/write those two fields to also maintain a third one -
    /// the same drift the legacy <c>FNAME + ' ' + LNAME</c> join key produces, now prevented instead
    /// of reconciled after the fact.
    /// </summary>
    public string Name { get; private set; } = "";

    /// <summary>
    /// Satisfies <see cref="IMasterDataRecord" />'s public setter without opening one on
    /// <see cref="Name" /> itself - <see cref="Name" /> stays computed-only from outside the class.
    /// </summary>
    string IMasterDataRecord.Name
    {
        get => Name;
        set => Name = value;
    }

    private void RecomputeName() => Name = string.Join(" ", firstName, lastName).Trim();

    public string GetFullName() => Name;

    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The person's position as the company names it ("Cold Chain Asst. Manager"). A display label
    /// only - access comes from <see cref="Role" />, never from this.
    /// </summary>
    public string? JobTitle { get; set; }

    /// <summary>
    /// Personal address. Required for Stripe Connect onboarding when the employee's country differs
    /// from the tenant's; otherwise the tenant's company address is used as a fallback.
    /// </summary>
    public Address? Address { get; set; }

    public Money Salary { get; set; } = Money.Zero("USD");
    public SalaryType SalaryType { get; set; } = SalaryType.None;

    private EmployeeStatus status = EmployeeStatus.Active;

    /// <summary>
    /// The HR lifecycle (active / on leave / suspended / terminated) - independent of
    /// <see cref="IsActive" />, the master-data "still current" flag, except for one deliberate,
    /// one-way link: terminating an employee also deactivates the master record, because a
    /// terminated employee should stop being offered anywhere a steward picks staff from. Nothing
    /// reverses that automatically - un-terminating (setting <see cref="EmployeeStatus.Active" />
    /// again, e.g. a rehire) does not flip <see cref="IsActive" /> back on its own; a steward
    /// reactivates the record deliberately, the same way restoring a soft-deleted record is its own
    /// action. <see cref="EmployeeStatus.OnLeave" /> and <see cref="EmployeeStatus.Suspended" />
    /// never touch <see cref="IsActive" /> - both describe someone still on the roster.
    /// </summary>
    public EmployeeStatus Status
    {
        get => status;
        set
        {
            status = value;

            if (value == EmployeeStatus.Terminated)
            {
                IsActive = false;
            }
        }
    }

    /// <summary>For drivers: regular or on-call. Null for office staff, or when not recorded.</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>
    /// For drivers: the unit they normally work for (Cold Chain, wing van, trucking). Null for office
    /// staff, or when not recorded.
    /// </summary>
    public CrewUnit? HomeUnit { get; set; }

    /// <summary>
    ///     When employee joined to this tenant
    /// </summary>
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     User's device token generated by the Firebase
    /// </summary>
    public string? DeviceToken { get; set; }

    public virtual List<EmployeeDocument> Documents { get; set; } = [];

    /// <summary>
    ///     Driver licenses (history). A driver can hold multiple licenses across jurisdictions
    ///     or accumulated renewals.
    /// </summary>
    public virtual List<DriverLicense> Licenses { get; set; } = [];

    /// <summary>
    ///     Dispatched loads by dispatchers
    /// </summary>
    public virtual List<Load> DispatchedLoads { get; } = [];

    /// <summary>
    ///     Stripe Connected Account ID for receiving payroll payouts via bank transfer.
    /// </summary>
    public string? StripeConnectedAccountId { get; set; }

    public virtual List<PayrollInvoice> PayrollInvoices { get; set; } = [];

    /// <summary>
    ///     Time entries for hourly employees
    /// </summary>
    public virtual List<TimeEntry> TimeEntries { get; set; } = [];

    /// <summary>
    ///     User tenant role
    /// </summary>
    public Guid? RoleId { get; set; }
    public virtual TenantRole? Role { get; set; }

    /// <summary>
    /// Owning branch, or null when the employee is not scoped to one (e.g. a role that works across
    /// offices). Scoping governs stewardship, not visibility or assignment eligibility.
    /// </summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the employee still belongs on the master roster. Distinct from <see cref="Status" />
    /// (see its doc comment for the one-way link) and from deletion: an inactive employee keeps
    /// every load, license and payroll record they were ever part of.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public static Employee CreateEmployeeFromUser(
        User user,
        decimal salary = 0,
        SalaryType salaryType = SalaryType.None)
    {
        var newEmployee = new Employee
        {
            Id = user.Id,
            Code = "",
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Salary = new() { Amount = salary, Currency = "USD" },
            SalaryType = salaryType
        };

        return newEmployee;
    }
}
