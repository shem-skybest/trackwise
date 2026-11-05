using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Raw GPS breadcrumb for a truck. Kept 4 years (the IFTA audit window) as the evidence
/// trail behind <see cref="TruckJurisdictionMileage"/> rollups; deliberately lean (not
/// auditable) - ~288 rows/truck/day at a 5-minute ping rate. Consider monthly partitioning
/// if a tenant exceeds ~500 trucks.
/// </summary>
public class TruckLocationHistory : Entity, ITenantEntity
{
    public required Guid TruckId { get; set; }
    public virtual Truck Truck { get; set; } = null!;

    public required GeoPoint Location { get; set; }

    /// <summary>When the position was recorded (UTC).</summary>
    public required DateTime Timestamp { get; set; }

    /// <summary>ELD provider the ping came from; null for driver-app/manual sources.</summary>
    public EldProviderType? Source { get; set; }

    public int? OdometerReading { get; set; }
}
