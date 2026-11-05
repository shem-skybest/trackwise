using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A ship, as a canonical record rather than a name somebody typed.
/// <para>
/// The purest case the module exists for: there is no legacy vessel table anywhere to migrate.
/// nSFS keeps <c>VesselName</c> as free text on <c>TBL_CUSTOMER_SERVICE</c> and TrackWise copied
/// the habit onto <c>Workfile.VesselName</c>, so every misspelling any clerk ever made is a
/// distinct "vessel" today. This table is the first place a ship has an identity.
/// </para>
/// </summary>
public class Vessel : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.ReferenceList;

    /// <summary>
    /// The stable handle other systems join on. Blank on create means "allocate from the
    /// <c>Vessel</c> number series"; immutable afterwards, like every master code.
    /// <para>
    /// Deliberately not the IMO number. Barges and domestic craft have no IMO at all, so using it
    /// as the code would leave exactly the records this list is worst at - the local ones - without
    /// a handle.
    /// </para>
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a vessel code on write. Callers querying by code must normalise the search
    /// term through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    /// <summary>
    /// IMO ship identification number - seven digits, globally unique, and never reassigned even
    /// when a ship is renamed or reflagged. Where a vessel has one it is the strongest duplicate
    /// signal in the module, which is why the probe matches it exactly.
    /// <para>
    /// Nullable because most of the domestic fleet genuinely has none. Stored digits-only through
    /// <see cref="NormalizeImoNumber"/> so "IMO 9395044" and "9395044" are one vessel.
    /// </para>
    /// </summary>
    public string? ImoNumber
    {
        get => imoNumber;
        set => imoNumber = NormalizeImoNumber(value);
    }

    private string? imoNumber;

    /// <summary>
    /// Strips the "IMO" prefix, spaces and punctuation an operator's paperwork puts around the
    /// number, leaving the digits. Returns null for a blank value so the column stays genuinely
    /// empty rather than holding an empty string the exact-match probe would treat as a value.
    /// </summary>
    public static string? NormalizeImoNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string([.. value.Where(char.IsAsciiDigit)]);
        return digits.Length == 0 ? null : digits;
    }

    /// <summary>Radio call sign, where the operator supplies one. Free-form: formats vary by flag.</summary>
    public string? CallSign { get; set; }

    /// <summary>ISO 3166-1 alpha-2 country of registry, e.g. "PH", "PA", "LR".</summary>
    public string? FlagCountryCode { get; set; }

    public required VesselType Type { get; set; }

    /// <summary>
    /// Nominal TEU capacity, for the container-carrying types. Null where it means nothing - a
    /// tanker has no TEU figure, and storing zero would read as "we know it is none".
    /// </summary>
    public int? TeuCapacity { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Owning branch, or null when every branch uses this vessel. Scoping governs stewardship, not
    /// visibility - a ship one office books is still readable everywhere.
    /// </summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the vessel is still offered for new work. Distinct from deletion: a ship that leaves
    /// the service keeps every workfile that ever named it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
