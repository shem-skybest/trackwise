using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// What a billing batch needs to know about one trip, read in one query for the whole period rather
/// than through each trip's navigations.
/// </summary>
/// <param name="BillingDate">The day the trip counts for: its rating date, else the local day it completed.</param>
/// <param name="Amount">The priced truck-trip total; null until the trip is priced.</param>
public sealed record BillableTrip(
    Guid TripId,
    long TripNumber,
    Guid CustomerId,
    TripStatus Status,
    DateOnly BillingDate,
    string? TruckNumber,
    string? BookingReferences,
    int Drops,
    int FoulTripDrops,
    int CancelledDrops,
    int PartialDrops,
    int? PlannedBoxes,
    string? RateLineLabel,
    decimal? BaseAmount,
    decimal? FuelAmount,
    decimal? ExtraDropAmount,
    decimal? Amount,
    bool IsPriceOutdated)
{
    /// <summary>Why the trip can't be billed as it stands, or null when it can.</summary>
    public string? Problem => BillingBatchTrip.ProblemOf(Status, Amount, IsPriceOutdated);
}
