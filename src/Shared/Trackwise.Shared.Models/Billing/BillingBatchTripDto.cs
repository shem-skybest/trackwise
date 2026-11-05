using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

public record BillingBatchTripDto
{
    public Guid TripId { get; set; }
    public long TripNumber { get; set; }
    public TripStatus TripStatus { get; set; }
    public DateOnly BillingDate { get; set; }
    public string? TruckNumber { get; set; }
    public string? BookingReferences { get; set; }
    public int Drops { get; set; }
    public int FoulTripDrops { get; set; }
    public int CancelledDrops { get; set; }
    public int PartialDrops { get; set; }
    public int? PlannedBoxes { get; set; }
    public string? RateLineLabel { get; set; }
    public decimal? BaseAmount { get; set; }
    public decimal? FuelAmount { get; set; }
    public decimal? ExtraDropAmount { get; set; }
    public decimal? Amount { get; set; }
    public bool IsPriceOutdated { get; set; }

    /// <summary>Why the trip can't be billed yet; null when it can.</summary>
    public string? Problem { get; set; }
}
