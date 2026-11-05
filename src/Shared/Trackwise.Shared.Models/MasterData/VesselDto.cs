using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>Read model for a vessel - the canonical record behind what used to be a typed name.</summary>
public class VesselDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    /// <summary>Digits only. Null for the domestic craft that genuinely have no IMO number.</summary>
    public string? ImoNumber { get; set; }

    public string? CallSign { get; set; }
    public string? FlagCountryCode { get; set; }
    public VesselType Type { get; set; }
    public int? TeuCapacity { get; set; }
    public string? Notes { get; set; }

    /// <summary>Owning branch, or null when the vessel applies to every branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this vessel into another. Non-null means the record is a tombstone
    /// that still resolves, not a live vessel.
    /// </summary>
    public Guid? MergedIntoId { get; set; }

    /// <summary>Non-null for a soft-deleted record. Only ever populated on a deleted-records view.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set when the caller asked for an id that had been merged away and this record is the
    /// survivor it forwarded to. Lets a client say "that vessel is now this one" rather than
    /// silently showing something the user did not ask for.
    /// </summary>
    public Guid? ForwardedFromId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
