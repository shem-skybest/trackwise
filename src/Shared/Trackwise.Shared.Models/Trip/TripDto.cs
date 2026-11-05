using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public record TripDto
{
    public Guid Id { get; set; }
    public long Number { get; set; }
    public required string Name { get; set; }

    public required Address OriginAddress { get; set; }
    public required Address DestinationAddress { get; set; }

    /// <summary>
    ///     Total distance of the trip in kilometers.
    /// </summary>
    public double TotalDistance { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? GarageDepartedAt { get; set; }
    public DateTime? GarageArrivedAt { get; set; }
    public DateTime? GarageDepartureRecordedAt { get; set; }
    public TripEventSource? GarageDepartureSource { get; set; }
    public DateTime? GarageArrivalRecordedAt { get; set; }
    public TripEventSource? GarageArrivalSource { get; set; }
    public TripStatus Status { get; set; }
    public Guid? TruckId { get; set; }
    public string? TruckNumber { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public Guid? HelperId { get; set; }
    public string? HelperName { get; set; }
    public Guid? SecondHelperId { get; set; }
    public string? SecondHelperName { get; set; }

    /// <summary>
    ///     Number of loads in the trip.
    /// </summary>
    public int LoadsCount { get; set; }

    /// <summary>
    ///     Total revenue from all loads in the trip.
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>Boxes planned across the trip's loads - summed, never entered.</summary>
    public int TotalPlannedBoxes { get; set; }

    /// <summary>Bags planned across the trip's loads - summed, never entered.</summary>
    public int TotalPlannedBags { get; set; }

    /// <summary>The rate agreement the trip was priced on; null until it is priced.</summary>
    public Guid? RateAgreementId { get; set; }

    public string? RateLineLabel { get; set; }
    public decimal? RateBaseAmount { get; set; }
    public decimal? RateFuelAmount { get; set; }
    public string? RateFuelBasis { get; set; }
    public int? RateExtraDrops { get; set; }
    public decimal? RateExtraDropAmount { get; set; }
    public decimal? RateTotal { get; set; }
    public DateOnly? RatedOn { get; set; }
    public DateTime? RatedAt { get; set; }

    /// <summary>A drop's outcome was corrected after the trip was priced, so the price needs redoing.</summary>
    public bool IsPriceOutdated { get; set; }

    public IEnumerable<TripStopDto> Stops { get; set; } = [];

    public IEnumerable<TripLoadDto> Loads { get; set; } = [];

    /// <summary>Other trucks that took over drops; filled on the single-trip read only.</summary>
    public IReadOnlyList<TripRescueDto> Rescues { get; set; } = [];
}
