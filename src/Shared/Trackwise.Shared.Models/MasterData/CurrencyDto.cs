namespace Trackwise.Shared.Models;

/// <summary>Read model for a currency - the list behind the code every Money already carries.</summary>
public class CurrencyDto
{
    public Guid Id { get; set; }

    /// <summary>ISO 4217 alpha-3. Never allocated from a number series; a standards body assigns it.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    /// <summary>ISO 4217 numeric code, digits only.</summary>
    public string? NumericCode { get; set; }

    public string? Symbol { get; set; }

    /// <summary>Decimal places the currency subdivides into - 2 for most, 0 for the yen.</summary>
    public int MinorUnits { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this currency into another. Non-null means the record is a
    /// tombstone that still resolves.
    /// </summary>
    public Guid? MergedIntoId { get; set; }

    /// <summary>Non-null for a soft-deleted record. Only ever populated on a deleted-records view.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set when the caller asked for an id that had been merged away and this record is the
    /// survivor it forwarded to.
    /// </summary>
    public Guid? ForwardedFromId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
