using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>Read model for a container or equipment type.</summary>
public class ContainerTypeDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }

    /// <summary>ISO 6346 size-type code. Null for the equipment the standard does not describe.</summary>
    public string? IsoSizeTypeCode { get; set; }

    public ContainerTypeCategory Category { get; set; }
    public int? LengthFeet { get; set; }
    public bool IsHighCube { get; set; }
    public decimal? TeuFactor { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this type into another. Non-null means the record is a tombstone
    /// that still resolves, not a live type.
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
