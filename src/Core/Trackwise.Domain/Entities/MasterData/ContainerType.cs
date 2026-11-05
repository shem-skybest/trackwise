using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A container or equipment type - the box a shipment moves in, as one canonical record.
/// <para>
/// Unlike every list before it, this one is not a new table filling an empty space: it is a
/// <b>reconciliation</b>. Two enums already described these things in two vocabularies on two
/// different aggregates - <c>ContainerIsoType</c> (<c>Gp20</c>, ISO 6346's shorthand, on
/// <c>Container</c>) and <c>EquipmentType</c> (<c>DRY_VAN_20</c>, the load board's, on
/// <c>Booking</c> and <c>Tariff</c>) - and neither was a superset of the other. ISO had no
/// 40' high-cube reefer; the load board had no 45' high cube, and called a road tanker and a tank
/// container the same thing.
/// </para>
/// <para>
/// This record is the union, and the two retired vocabularies survive as
/// <see cref="MasterDataXref"/> rows so a legacy value still resolves to the row it became. That is
/// the whole point of picking a winner rather than keeping both: a booking for a
/// <c>DRY_VAN_40</c> and a container of <c>Gp40</c> are now the same row, and can be counted
/// together for the first time.
/// </para>
/// <para>
/// Deliberately <b>not</b> <c>IBranchScoped</c>. ISO 6346 does not vary by office - a 40' high
/// cube is the same box in Manila and Cebu - so scoping it would only offer stewards a way to
/// create the drift this module exists to remove. See <c>Currency</c>, which is global for the
/// same reason.
/// </para>
/// </summary>
public class ContainerType : AuditableEntity, ITenantEntity, IMasterDataRecord
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.ReferenceList;

    /// <summary>
    /// The stable handle other systems join on - <c>GP20</c>, <c>HC40</c>, <c>RF40HC</c>. Blank on
    /// create means "allocate from the <c>ContainerType</c> number series", though in practice a
    /// steward types the industry shorthand, because it is the name everyone on a yard uses.
    /// <para>
    /// Deliberately not the ISO size-type code: a road tanker and bulk cargo have none, and using
    /// it as the code would leave exactly the non-ISO equipment without a handle - the same
    /// reasoning that keeps <see cref="Vessel.Code"/> off the IMO number.
    /// </para>
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a container-type code on write. Callers querying by code must normalise the
    /// search term through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    /// <summary>
    /// ISO 6346 size-type code - four characters, e.g. <c>22G1</c> for a 20' general purpose box.
    /// Globally defined and unambiguous, so where a type has one it is the strongest duplicate
    /// signal available and the probe matches it exactly, exactly as it does a vessel's IMO number.
    /// <para>
    /// Nullable because the non-ISO equipment genuinely has none: a road tanker and a bulk movement
    /// are not containers and the standard does not describe them. Unique only among live rows
    /// <i>where present</i>, which is a filtered index rather than a unique constraint.
    /// </para>
    /// </summary>
    public string? IsoSizeTypeCode
    {
        get => isoSizeTypeCode;
        set => isoSizeTypeCode = NormalizeIsoSizeTypeCode(value);
    }

    private string? isoSizeTypeCode;

    /// <summary>
    /// Strips the spaces and hyphens paperwork writes an ISO size-type code with and upper-cases
    /// what is left, so "22 G1" and "22g1" are one code. Returns null for a blank value, so the
    /// column stays genuinely empty rather than holding an empty string the exact-match probe
    /// would treat as a value.
    /// </summary>
    public static string? NormalizeIsoSizeTypeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = new string([.. value.Where(char.IsAsciiLetterOrDigit)]).ToUpperInvariant();
        return cleaned.Length == 0 ? null : cleaned;
    }

    public required ContainerTypeCategory Category { get; set; }

    /// <summary>
    /// Nominal external length in feet - 20, 40, 45. Null for the equipment that has no standard
    /// length, which is the road tanker and bulk: storing zero would read as "we know it is none".
    /// </summary>
    public int? LengthFeet { get; set; }

    /// <summary>
    /// A high cube is a foot taller than the standard box of the same length. Separate from
    /// <see cref="LengthFeet"/> because it is the dimension that decides whether a chassis and a
    /// bridge clearance work, and folding it into the length would lose it.
    /// </summary>
    public bool IsHighCube { get; set; }

    /// <summary>
    /// Twenty-foot equivalent units - 1.0 for a 20', 2.0 for a 40', 2.25 for a 45'. What vessel and
    /// yard capacity is actually counted in. Null where the type is not a container and the figure
    /// would be an invention.
    /// </summary>
    public decimal? TeuFactor { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Whether the type is still offered for new work. Distinct from deletion: a box the company
    /// stops handling keeps every container, booking and tariff that ever named it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
