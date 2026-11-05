using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents a trip consisting of multiple stops for picking up and delivering loads.
/// </summary>
public partial class Trip : AuditableEntity, ITenantEntity
{
    /// <summary>
    /// Sequential number of the trip, unique within the tenant.
    /// </summary>
    public long Number { get; private set; }

    public required string Name { get; set; }

    /// <summary>
    /// Total distance of the trip in kilometers.
    /// </summary>
    public double TotalDistance { get; set; }

    public DateTime? DispatchedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    /// <summary>When the truck left the garage for this trip - the reported time. See <see cref="RecordGarageTimes" />.</summary>
    public DateTime? GarageDepartedAt { get; private set; }

    /// <summary>When the truck got back to the garage after this trip - the reported time.</summary>
    public DateTime? GarageArrivedAt { get; private set; }

    public TripStatus Status { get; private set; } = TripStatus.Draft;

    public Guid? TruckId { get; set; }
    public virtual Truck? Truck { get; set; }

    /// <summary>
    /// Who drives this trip. Crew are paired with a truck per day, not permanently - most units rotate
    /// among several drivers - so the trip records its own driver rather than reading the truck's
    /// main driver, which is only the default. See <see cref="AssignCrew" />.
    /// </summary>
    public Guid? DriverId { get; private set; }

    public virtual Employee? Driver { get; private set; }

    /// <summary>
    /// The helper riding this trip, if any - one per trip/day, not per stop, matching how a helper
    /// is actually assigned in practice.
    /// </summary>
    public Guid? HelperId { get; private set; }

    public virtual Helper? Helper { get; private set; }

    /// <summary>A second helper, for the occasional truck that runs with two.</summary>
    public Guid? SecondHelperId { get; private set; }

    public virtual Helper? SecondHelper { get; private set; }

    public virtual List<TripStop> Stops { get; } = [];

    /// <summary>
    /// Gets the origin address (first stop).
    /// </summary>
    public Address GetOriginAddress()
    {
        return Stops.OrderBy(s => s.Order).First().Address;
    }

    /// <summary>
    /// Gets the destination address (last stop).
    /// </summary>
    public Address GetDestinationAddress()
    {
        return Stops.OrderBy(s => s.Order).Last().Address;
    }

    /// <summary>
    /// Calculates the total revenue from all loads in the trip.
    /// </summary>
    public decimal CalcTotalRevenue()
    {
        return Stops
            .Where(s => s.Type == TripStopType.DropOff)
            .Sum(s => s.Load.DeliveryCost.Amount);
    }

    /// <summary>
    /// Calculates the total driver share from all loads in the trip.
    /// </summary>
    public decimal CalcDriversShare()
    {
        return Stops
            .Where(s => s.Type == TripStopType.DropOff)
            .Sum(s => s.Load.CalcDriverShare());
    }
}
