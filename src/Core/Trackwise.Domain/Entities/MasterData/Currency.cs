using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A currency, as the maintainable list behind the three-letter code every <c>Money</c> already
/// carries.
/// <para>
/// The easy half of a hard problem, and the half worth doing now. What the brief asks for is an FX
/// table, and an FX rate is <b>effective-dated</b>: "what is USD to PHP" has no answer without a
/// date, and answering it needs the rate machinery that arrives with tariffs. Building the table
/// here would either drag that forward or produce something that cannot answer "what was the rate
/// in March" - which is the only question anyone asks it. So this is the list alone, and
/// <c>EffectiveDated</c> stays false on its policy until the rates step gives it meaning.
/// </para>
/// <para>
/// Deliberately <b>not</b> <c>IBranchScoped</c>: ISO 4217 does not vary by office, and a currency
/// per branch is drift with extra steps. Same reasoning as <see cref="ContainerType"/>.
/// </para>
/// </summary>
public class Currency : AuditableEntity, ITenantEntity, IMasterDataRecord
{
    private string code = "";

    public MasterDataGroup Group => MasterDataGroup.ReferenceList;

    /// <summary>
    /// ISO 4217 alpha-3 code - <c>USD</c>, <c>PHP</c>, <c>EUR</c>. The one master code in the
    /// module that is <b>never</b> allocated from the number series: a currency's identity is
    /// assigned by a standards body, so a blank code here is an omission rather than a request for
    /// one, and the validator refuses it.
    /// <para>
    /// This is also what makes the list immediately useful without a foreign key anywhere: every
    /// <c>Money.Currency</c> in the schema already holds exactly this value as text.
    /// </para>
    /// </summary>
    public required string Code
    {
        get => code;
        set => code = NormalizeCode(value);
    }

    /// <summary>
    /// Canonicalises a currency code on write. Callers querying by code must normalise the search
    /// term through this too - the stored value is always canonical.
    /// </summary>
    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    public required string Name { get; set; }

    /// <summary>
    /// ISO 4217 numeric code - three digits, e.g. <c>608</c> for the peso. Globally unique and
    /// assigned alongside the alpha code, so the probe matches it exactly as a second identifier,
    /// the same way it does a vessel's IMO number. Nullable: a steward adding a currency by its
    /// familiar letters should not be blocked on looking the number up.
    /// </summary>
    public string? NumericCode
    {
        get => numericCode;
        set => numericCode = NormalizeDigits(value);
    }

    private string? numericCode;

    /// <summary>
    /// Leaves the digits of a numeric code. Returns null for a blank value so the column stays
    /// genuinely empty rather than holding an empty string the exact-match probe would treat as a
    /// value.
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

    /// <summary>Display symbol - "$", "€", "₱". Presentation only; never parsed.</summary>
    public string? Symbol { get; set; }

    /// <summary>
    /// How many decimal places the currency subdivides into: 2 for most, 0 for the yen, 3 for the
    /// dinar. Recorded because rounding a total to two places is wrong in both of those directions,
    /// and the alternative is every caller assuming two.
    /// </summary>
    public int MinorUnits { get; set; } = 2;

    public string? Notes { get; set; }

    /// <summary>
    /// Whether the currency is still offered for new work. Distinct from deletion: a currency the
    /// company stops quoting in keeps every invoice ever denominated in it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
