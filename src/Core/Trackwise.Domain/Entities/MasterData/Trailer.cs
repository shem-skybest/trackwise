using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A trailer, as a canonical record. No prior entity exists for this in the domain - a trailer has
/// never had an identity here before, the same starting point <see cref="Vessel"/> had.
/// <para>
/// Deliberately carries no link to <see cref="Truck"/> (e.g. "currently pulled by"): wiring that up
/// means touching the Truck/Operations side, which waits for Truck's own promotion into this module.
/// This stays a pure identity register until then, the same way Vessel was before anything
/// referenced it.
/// </para>
/// </summary>
public class Trailer : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.FleetAndPersonnel;

    /// <summary>
    /// The stable handle other systems join on. Blank on create means "allocate from the
    /// <c>Trailer</c> number series"; immutable afterwards, like every master code.
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a trailer code on write. Callers querying by code must normalise the search
    /// term through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    public required TrailerType Type { get; set; }

    /// <summary>
    /// Vehicle Identification Number - 17 characters, globally unique and never reassigned. Where a
    /// trailer has one on file it is the strongest duplicate signal in the list, the same role
    /// <c>Vessel.ImoNumber</c> plays for ships.
    /// <para>
    /// Nullable because not every trailer has one recorded. Stored upper-trimmed through
    /// <see cref="NormalizeVin"/> so "1gr" and "1GR" are the same value to the duplicate probe.
    /// </para>
    /// </summary>
    public string? Vin
    {
        get => vin;
        set => vin = NormalizeVin(value);
    }

    private string? vin;

    /// <summary>
    /// Trims and upper-cases a VIN, returning null for a blank value so the column stays genuinely
    /// empty rather than holding an empty string the exact-match probe would treat as a value.
    /// </summary>
    public static string? NormalizeVin(string? value)
    {
        var trimmed = value?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public string? LicensePlate { get; set; }

    /// <summary>State or province the plate is registered in, e.g. "TX", "CA".</summary>
    public string? LicensePlateState { get; set; }

    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }

    public int? AxleCount { get; set; }

    /// <summary>Maximum load capacity in kilograms. Null where it has not been recorded.</summary>
    public decimal? MaxLoadCapacityKg { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Owning branch, or null when every branch uses this trailer. Scoping governs stewardship, not
    /// visibility - a trailer one office maintains is still readable everywhere.
    /// </summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the trailer is still offered for new work. Distinct from deletion: a trailer taken out
    /// of service keeps every load it ever carried.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
