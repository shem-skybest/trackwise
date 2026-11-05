using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A genset unit, as a canonical record. No prior entity exists for this in the domain - a genset
/// has never had an identity here before, the same starting point <see cref="Trailer"/> had.
/// <para>
/// A genset is a scarce shared physical resource (see the Cold Chain handoff doc, §3): it is
/// optionally attached to a reefer container booking, never booked standalone, and its own
/// maintenance/repair history is tracked separately from the trucks and trailers it rides with.
/// This stays a pure identity + availability register - the reefer-workfile attachment and its
/// billing shape are a Workfile-side concern, not this entity's.
/// </para>
/// </summary>
public class Genset : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.FleetAndPersonnel;

    /// <summary>
    /// The stable handle other systems join on. Blank on create means "allocate from the
    /// <c>Genset</c> number series"; immutable afterwards, like every master code.
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a genset code on write. Callers querying by code must normalise the search
    /// term through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    public required GensetStatus Status { get; set; }

    /// <summary>
    /// Manufacturer serial number - the strongest duplicate signal in the list, the same role
    /// <c>Trailer.Vin</c> plays for a trailer. Nullable because not every unit has one recorded.
    /// Stored upper-trimmed through <see cref="NormalizeSerialNumber"/> so "sn-1" and "SN-1" are the
    /// same value to the duplicate probe.
    /// </summary>
    public string? SerialNumber
    {
        get => serialNumber;
        set => serialNumber = NormalizeSerialNumber(value);
    }

    private string? serialNumber;

    /// <summary>
    /// Trims and upper-cases a serial number, returning null for a blank value so the column stays
    /// genuinely empty rather than holding an empty string the exact-match probe would treat as a
    /// value.
    /// </summary>
    public static string? NormalizeSerialNumber(string? value)
    {
        var trimmed = value?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Owning branch, or null when every branch uses this genset. Scoping governs stewardship, not
    /// visibility - a genset one office maintains is still readable everywhere.
    /// </summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the genset is still offered for assignment. Distinct from deletion: a genset taken
    /// out of service keeps every booking it was ever attached to.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
