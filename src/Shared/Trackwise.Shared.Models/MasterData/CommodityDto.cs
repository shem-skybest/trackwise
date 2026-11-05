namespace Trackwise.Shared.Models;

/// <summary>Read model for a commodity - the canonical record behind what used to be a typed phrase.</summary>
public class CommodityDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    /// <summary>Harmonized System tariff code, digits only. Shared by design - several commodities can carry one.</summary>
    public string? HsCode { get; set; }

    public string? Description { get; set; }
    public bool IsHazardous { get; set; }
    public string? UnNumber { get; set; }
    public bool RequiresTemperatureControl { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when the commodity applies to every branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this commodity into another. Non-null means the record is a
    /// tombstone that still resolves, not a live commodity.
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
