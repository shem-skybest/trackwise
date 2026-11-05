using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// What the company's vehicle register keeps about a unit beyond dispatch: its internal asset code,
/// how and when it was acquired, and the identifiers printed on its registration.
/// </summary>
public partial class Truck
{
    private string? assetCode;
    private string? conductionSticker;
    private string? engineNumber;

    /// <summary>
    /// The company's own asset tag, e.g. <c>R230325</c> (type letter, sequence, purchase month and
    /// year). Distinct from <see cref="Number" />, which is what dispatch calls the unit.
    /// </summary>
    public string? AssetCode
    {
        get => assetCode;
        set => assetCode = NormalizeIdentifier(value);
    }

    /// <summary>
    /// The temporary LTO sticker a new unit runs on before its plate is issued, e.g. <c>RK861A</c>.
    /// Kept after the plate arrives because older paperwork still carries it.
    /// </summary>
    public string? ConductionSticker
    {
        get => conductionSticker;
        set => conductionSticker = NormalizeIdentifier(value);
    }

    /// <summary>The engine number printed on the CR. The chassis number is <see cref="Vin" />.</summary>
    public string? EngineNumber
    {
        get => engineNumber;
        set => engineNumber = NormalizeIdentifier(value);
    }

    /// <summary>When the company bought the unit - not the model <see cref="Year" />.</summary>
    public DateOnly? AcquiredOn { get; set; }

    /// <summary>The dealer or auction house the unit was bought from.</summary>
    public string? Supplier
    {
        get => supplier;
        set => supplier = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private string? supplier;

    public TruckOwnership? Ownership { get; set; }

    private static string? NormalizeIdentifier(string? value)
    {
        var trimmed = value?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
