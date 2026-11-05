using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A laborer who assists a driver - loading, unloading, escorting an oversized load - as a
/// canonical record.
/// <para>
/// Deliberately not built on <see cref="Employee"/>. Employee shares its primary key with the
/// master-database <see cref="User"/> and carries payroll, Stripe Connect and invitation machinery
/// a helper does not need - giving Helper that shape would import the same auth entanglement that
/// is the reason Employee's own promotion into this module is a separate, later step. Helper stays
/// a standalone lightweight personnel record: an identity, a way to reach them, nothing more.
/// </para>
/// </summary>
public class Helper : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.FleetAndPersonnel;

    /// <summary>
    /// The stable handle other systems join on. Blank on create means "allocate from the
    /// <c>Helper</c> number series"; immutable afterwards.
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a helper code on write. Callers querying by code must normalise the search term
    /// through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    public string? PhoneNumber { get; set; }

    /// <summary>Home address, where one has been recorded. Not every helper has one on file.</summary>
    public Address? Address { get; set; }

    public string? Notes { get; set; }

    /// <summary>Regular or on-call; null when not recorded.</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>The unit they normally work for (Cold Chain, wing van, trucking); null when not recorded.</summary>
    public CrewUnit? HomeUnit { get; set; }

    /// <summary>Owning branch, or null when every branch uses this helper.</summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the helper is still offered for assignment. Distinct from deletion: someone no longer
    /// engaged keeps every load they were ever assigned to.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
