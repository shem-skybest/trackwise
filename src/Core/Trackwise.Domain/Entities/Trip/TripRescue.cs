using Trackwise.Domain.Core;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Another truck taking over a trip's remaining drops after a breakdown or accident - Cold Chain's
/// "rescue unit". The goods are moved across and the rescue truck finishes the deliveries, but the
/// trip stays the client's truck-trip: it keeps its truck, booking and price, and each drop the
/// rescue took over points here so it is clear who delivered it. See <see cref="Trip.RecordRescue" />.
/// </summary>
public class TripRescue : Entity, ITenantEntity
{
    public Guid TripId { get; private set; }

    public Guid TruckId { get; private set; }
    public Guid? DriverId { get; private set; }
    public Guid? HelperId { get; private set; }
    public Guid? SecondHelperId { get; private set; }

    /// <summary>When the goods were moved to the rescue truck - the reported time, in UTC.</summary>
    public DateTime TransferredAt { get; private set; }

    /// <summary>Where the goods were moved across, in words.</summary>
    public string? Place { get; private set; }

    public string Reason { get; private set; } = null!;

    /// <summary>The incident that called for the rescue, when one is on record.</summary>
    public Guid? IncidentId { get; private set; }

    /// <summary>When the rescue truck left its garage - the reported time. See <see cref="Trip.RecordRescueGarageTimes" />.</summary>
    public DateTime? GarageDepartedAt { get; private set; }

    /// <summary>When the rescue truck got back to its garage after the last drop it took over.</summary>
    public DateTime? GarageArrivedAt { get; private set; }

    public string? RecordedBy { get; private set; }
    public DateTime RecordedAt { get; private set; }

    internal static TripRescue Create(Guid tripId, RescueSpec spec, string? recordedBy, DateTime now) =>
        new()
        {
            TripId = tripId,
            TruckId = spec.Truck.Id,
            DriverId = spec.Driver.Id,
            HelperId = spec.Helper?.Id,
            SecondHelperId = spec.SecondHelper?.Id,
            TransferredAt = spec.TransferredAt,
            Place = string.IsNullOrWhiteSpace(spec.Place) ? null : spec.Place.Trim(),
            Reason = spec.Reason.Trim(),
            IncidentId = spec.IncidentId,
            RecordedBy = recordedBy,
            RecordedAt = now
        };

    internal void SetGarageTimes(DateTime? departedAt, DateTime? arrivedAt)
    {
        GarageDepartedAt = departedAt;
        GarageArrivedAt = arrivedAt;
    }
}
