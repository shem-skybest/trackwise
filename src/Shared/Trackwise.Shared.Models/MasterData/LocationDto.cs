using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>Read model for a location - delivery point, warehouse, yard, or the zone grouping them.</summary>
public class LocationDto
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public LocationType Type { get; set; }

    /// <summary>Null for a zone, which is a grouping rather than somewhere a truck goes.</summary>
    public Address? Address { get; set; }

    /// <summary>The zone this location sits in, or null when it belongs to none.</summary>
    public Guid? ParentLocationId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? ParentLocationName { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Notes { get; set; }

    /// <summary>The client this site belongs to, or null for a site any client can use.</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? CustomerName { get; set; }

    public TimeOnly? DefaultCutoffTime { get; set; }
    public int? ZoneRadiusMeters { get; set; }

    /// <summary>Owning branch, or null when the location applies to every branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Set when a steward merged this location into another. Non-null means the record is a
    /// tombstone that still resolves, not a live location.
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
