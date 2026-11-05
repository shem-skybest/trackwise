using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using TripState = Trackwise.Domain.Primitives.Enums.TripStatus;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One trip on a billing batch, with a copy of what it was charged. The copy follows the trip while
/// the batch is a draft and is frozen when the batch is locked, so the printed summary and what
/// Accounting bills can't drift from each other.
/// </summary>
public class BillingBatchTrip : Entity, ITenantEntity
{
    public Guid BillingBatchId { get; private set; }

    /// <summary>A trip is on at most one batch at a time.</summary>
    public Guid TripId { get; private set; }

    public virtual Trip Trip { get; private set; } = null!;

    public long TripNumber { get; private set; }
    public TripStatus TripStatus { get; private set; }
    public DateOnly BillingDate { get; private set; }
    public string? TruckNumber { get; private set; }

    /// <summary>The BKN-RFV bookings behind the trip, comma separated.</summary>
    public string? BookingReferences { get; private set; }

    public int Drops { get; private set; }
    public int FoulTripDrops { get; private set; }
    public int CancelledDrops { get; private set; }

    /// <summary>Drops where some boxes or bags came back.</summary>
    public int PartialDrops { get; private set; }

    public int? PlannedBoxes { get; private set; }
    public string? RateLineLabel { get; private set; }
    public decimal? BaseAmount { get; private set; }
    public decimal? FuelAmount { get; private set; }
    public decimal? ExtraDropAmount { get; private set; }

    /// <summary>The truck-trip charge; null while the trip isn't priced.</summary>
    public decimal? Amount { get; private set; }

    public bool IsPriceOutdated { get; private set; }

    /// <summary>Why this trip can't be billed as it stands, or null when it can.</summary>
    public string? Problem => ProblemOf(TripStatus, Amount, IsPriceOutdated);

    internal static string? ProblemOf(TripState status, decimal? amount, bool isPriceOutdated) =>
        status != TripState.Completed ? "The trip isn't completed."
        : amount is null ? "The trip isn't priced."
        : isPriceOutdated ? "The trip changed after it was priced; price it again."
        : null;

    internal static BillingBatchTrip Create(Guid batchId, BillableTrip trip)
    {
        var line = new BillingBatchTrip { BillingBatchId = batchId, TripId = trip.TripId };
        line.CopyFrom(trip);
        return line;
    }

    internal void CopyFrom(BillableTrip trip)
    {
        TripNumber = trip.TripNumber;
        TripStatus = trip.Status;
        BillingDate = trip.BillingDate;
        TruckNumber = trip.TruckNumber;
        BookingReferences = trip.BookingReferences;
        Drops = trip.Drops;
        FoulTripDrops = trip.FoulTripDrops;
        CancelledDrops = trip.CancelledDrops;
        PartialDrops = trip.PartialDrops;
        PlannedBoxes = trip.PlannedBoxes;
        RateLineLabel = trip.RateLineLabel;
        BaseAmount = trip.BaseAmount;
        FuelAmount = trip.FuelAmount;
        ExtraDropAmount = trip.ExtraDropAmount;
        Amount = trip.Amount;
        IsPriceOutdated = trip.IsPriceOutdated;
    }
}
