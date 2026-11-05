using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
///     Represents a stop on a trip.
///     A stop can be a pickup or a drop-off location.
///     A trip can have multiple stops.
/// </summary>
public class TripStop : Entity, ITenantEntity
{
    public TripStopType Type { get; set; }
    public Guid TripId { get; set; }
    public virtual required Trip Trip { get; set; }

    /// <summary>
    ///     1-based position in the overall route.
    /// </summary>
    public int Order { get; set; }

    public required Address Address { get; set; }
    public required GeoPoint Location { get; set; }

    /// <summary>
    ///     When the truck actually arrived - the reported time, which office staff often enter
    ///     hours later from a Messenger update. <see cref="ArrivalRecordedAt" /> is when it was
    ///     entered.
    /// </summary>
    public DateTime? ArrivedAt { get; set; }

    /// <summary>When the truck actually left - the reported time, like <see cref="ArrivedAt" />.</summary>
    public DateTime? DepartedAt { get; set; }

    /// <summary>
    ///     When loading (at a pickup) or unloading (at a drop) started - the reported time. Arrival to
    ///     this is waiting; this to <see cref="HandlingFinishedAt" /> is handling.
    /// </summary>
    public DateTime? HandlingStartedAt { get; set; }

    /// <summary>When loading or unloading finished - the reported time.</summary>
    public DateTime? HandlingFinishedAt { get; set; }

    /// <summary>When <see cref="ArrivedAt" /> was entered into the system.</summary>
    public DateTime? ArrivalRecordedAt { get; set; }

    /// <summary>Who entered <see cref="ArrivedAt" /> - a user name, or the integration that did.</summary>
    public string? ArrivalRecordedBy { get; set; }

    /// <summary>When <see cref="DepartedAt" /> was entered into the system.</summary>
    public DateTime? DepartureRecordedAt { get; set; }

    /// <summary>Who entered <see cref="DepartedAt" />.</summary>
    public string? DepartureRecordedBy { get; set; }

    /// <summary>Where <see cref="ArrivedAt" /> was entered; null on events recorded before sources were kept.</summary>
    public TripEventSource? ArrivalSource { get; set; }

    /// <summary>Where <see cref="DepartedAt" /> was entered.</summary>
    public TripEventSource? DepartureSource { get; set; }

    /// <summary>When <see cref="HandlingStartedAt" /> was entered into the system.</summary>
    public DateTime? HandlingStartedRecordedAt { get; set; }

    /// <summary>Where <see cref="HandlingStartedAt" /> was entered.</summary>
    public TripEventSource? HandlingStartedSource { get; set; }

    /// <summary>When <see cref="HandlingFinishedAt" /> was entered into the system.</summary>
    public DateTime? HandlingFinishedRecordedAt { get; set; }

    /// <summary>Where <see cref="HandlingFinishedAt" /> was entered.</summary>
    public TripEventSource? HandlingFinishedSource { get; set; }

    /// <summary>Reefer temperature (°C) at arrival - the cold-chain compliance capture.</summary>
    public decimal? TemperatureAtArrival { get; set; }

    /// <summary>Reefer temperature (°C) at departure.</summary>
    public decimal? TemperatureAtDeparture { get; set; }

    /// <summary>The rescue that took this drop over from the trip's own truck, if one did.</summary>
    public Guid? RescueId { get; set; }

    public Guid LoadId { get; set; }

    /// <summary>
    ///     The specific load handled at this stop.
    /// </summary>
    public virtual required Load Load { get; set; }

    /// <summary>
    ///     Get demand for this stop.
    ///     +1 for pickup, -1 for drop-off.
    /// </summary>
    public int GetDemand()
    {
        return Type is TripStopType.PickUp ? 1 : -1;
    }
}
