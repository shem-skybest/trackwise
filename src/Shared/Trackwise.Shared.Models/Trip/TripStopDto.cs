using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public record TripStopDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public TripStopType Type { get; set; }

    public required Address Address { get; set; }
    public required GeoPoint Location { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? DepartedAt { get; set; }
    public DateTime? ArrivalRecordedAt { get; set; }
    public string? ArrivalRecordedBy { get; set; }
    public DateTime? DepartureRecordedAt { get; set; }
    public string? DepartureRecordedBy { get; set; }

    /// <summary>Where each event was entered - the office or the crew's Driver App; null on older records.</summary>
    public TripEventSource? ArrivalSource { get; set; }
    public TripEventSource? DepartureSource { get; set; }
    public TripEventSource? HandlingStartedSource { get; set; }
    public TripEventSource? HandlingFinishedSource { get; set; }
    public DateTime? HandlingStartedRecordedAt { get; set; }
    public DateTime? HandlingFinishedRecordedAt { get; set; }
    public decimal? TemperatureAtArrival { get; set; }
    public decimal? TemperatureAtDeparture { get; set; }

    /// <summary>When loading (pickup) or unloading (drop) started.</summary>
    public DateTime? HandlingStartedAt { get; set; }

    /// <summary>When loading or unloading finished.</summary>
    public DateTime? HandlingFinishedAt { get; set; }

    /// <summary>A drop's cut-off - its load's requested delivery date and time.</summary>
    public DateTime? CutOff { get; set; }

    /// <summary>True when a drop arrived after its cut-off; null until both are known. Calculated, never entered.</summary>
    public bool? IsLate => Type == TripStopType.DropOff && ArrivedAt.HasValue && CutOff.HasValue
        ? ArrivedAt > CutOff
        : null;

    /// <summary>Minutes from arrival until loading/unloading started.</summary>
    public int? WaitingMinutes => MinutesBetween(ArrivedAt, HandlingStartedAt);

    /// <summary>Minutes spent loading or unloading.</summary>
    public int? HandlingMinutes => MinutesBetween(HandlingStartedAt, HandlingFinishedAt);

    /// <summary>Minutes from arrival to departure - the stop's dwell time.</summary>
    public int? DwellMinutes => MinutesBetween(ArrivedAt, DepartedAt);

    public Guid LoadId { get; set; }
    public Guid TripId { get; set; }

    /// <summary>The rescue that took this drop over, if one did - see <see cref="TripDto.Rescues" />.</summary>
    public Guid? RescueId { get; set; }

    public int GetDemand()
    {
        return Type is TripStopType.PickUp ? 1 : -1;
    }

    private static int? MinutesBetween(DateTime? from, DateTime? to) =>
        from.HasValue && to.HasValue ? (int)Math.Round((to.Value - from.Value).TotalMinutes) : null;
}
