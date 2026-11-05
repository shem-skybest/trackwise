using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// What is being moved, as a canonical record rather than a phrase somebody typed.
/// <para>
/// Like <see cref="Vessel"/>, there is no legacy table behind this one: nSFS and TrackWise both
/// keep the commodity as free text (<c>Workfile.Commodity</c>), so "electronics", "Electronic
/// parts" and "ELECT." are three commodities today and nothing can be counted by any of them.
/// </para>
/// </summary>
public class Commodity : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.ReferenceList;

    /// <summary>
    /// The stable handle other systems join on. Blank on create means "allocate from the
    /// <c>Commodity</c> number series"; immutable afterwards.
    /// <para>
    /// Kept separate from <see cref="HsCode"/> on purpose. An HS code classifies a whole family of
    /// goods for customs, so several distinct commodities legitimately share one - it identifies
    /// what a thing is for a tariff, not which record this is.
    /// </para>
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a commodity code on write. Callers querying by code must normalise the search
    /// term through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    /// <summary>
    /// Harmonized System tariff code, digits only, six to ten of them depending on how far the
    /// declaring country subdivides. Nullable: plenty of internal commodity lines never get
    /// classified, and a made-up code is worse than none.
    /// </summary>
    public string? HsCode
    {
        get => hsCode;
        set => hsCode = NormalizeDigits(value);
    }

    private string? hsCode;

    /// <summary>
    /// Strips the dots and spaces customs paperwork writes HS and UN numbers with, leaving digits.
    /// Returns null for a blank value so the column stays genuinely empty rather than holding an
    /// empty string the exact-match probe would treat as a value.
    /// </summary>
    public static string? NormalizeDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string([.. value.Where(char.IsAsciiDigit)]);
        return digits.Length == 0 ? null : digits;
    }

    public string? Description { get; set; }

    /// <summary>
    /// Dangerous goods. Drives nothing automatically yet - it is recorded so that the question is
    /// answered on the master record rather than re-asked, differently, on every booking.
    /// </summary>
    public bool IsHazardous { get; set; }

    /// <summary>
    /// UN number for dangerous goods, digits only. Only meaningful when
    /// <see cref="IsHazardous"/> is set; the validator enforces the pairing in that direction only,
    /// since a hazardous commodity can be awaiting classification.
    /// </summary>
    public string? UnNumber
    {
        get => unNumber;
        set => unNumber = NormalizeDigits(value);
    }

    private string? unNumber;

    /// <summary>Needs a reefer. Distinct from hazardous: cold chain is a handling requirement, not a risk class.</summary>
    public bool RequiresTemperatureControl { get; set; }

    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when every branch uses this commodity.</summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the commodity is still offered for new work. Distinct from deletion: a line the
    /// company stops carrying keeps every workfile that ever named it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
