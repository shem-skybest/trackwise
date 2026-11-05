using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The truck-trip price from the client's rate agreement. Kept as a snapshot - revising the
/// agreement later never changes what a past trip was charged.
/// </summary>
public partial class Trip
{
    public Guid? RateAgreementId { get; private set; }

    /// <summary>The rate card row the trip was charged on, e.g. "QUEZON CITY" or "41 - 60 KM".</summary>
    public string? RateLineLabel { get; private set; }

    public decimal? RateBaseAmount { get; private set; }
    public decimal? RateFuelAmount { get; private set; }

    /// <summary>How the fuel was worked out, e.g. "diesel ₱92.00, ₱85.00 tier".</summary>
    public string? RateFuelBasis { get; private set; }

    public int? RateExtraDrops { get; private set; }
    public decimal? RateExtraDropAmount { get; private set; }

    public DateOnly? RatedOn { get; private set; }
    public DateTime? RatedAt { get; private set; }

    public decimal? RateTotal => RateBaseAmount + RateFuelAmount + RateExtraDropAmount;

    /// <summary>
    /// The truck size the client booked, when every drop comes from Cold Chain bookings of one size.
    /// The trip is billed at that size whichever truck runs it: a 4-tonner sent on a 2-tonner booking
    /// still bills the 2-tonner rate.
    /// </summary>
    public TruckTonnageClass? BookedTruckSize =>
        Stops.Where(s => s.Type == TripStopType.DropOff)
            .Select(s => s.Load.Booking?.TruckSize)
            .Distinct()
            .ToList() is [{ } size]
            ? size
            : null;

    /// <summary>The size the rate card is read at: the booked size, or else the truck's own.</summary>
    public TruckTonnageClass? PricingSize => BookedTruckSize ?? Truck?.TonnageClass;

    /// <summary>
    /// Records the price and spreads it over the trip's drops as their delivery costs, so the
    /// trip's revenue (the sum of its loads) is the agreed truck-trip price rather than one charge
    /// per drop.
    /// </summary>
    /// <param name="ratedOn">The trip date the agreement and diesel price were read for.</param>
    public void ApplyRate(TripRateQuote quote, DateOnly ratedOn, DateTime ratedAt)
    {
        if (Status is TripStatus.Cancelled)
        {
            throw new InvalidOperationException("A cancelled trip can't be priced.");
        }

        var dropLoads = Stops
            .Where(s => s.Type == TripStopType.DropOff)
            .Select(s => s.Load)
            .ToList();

        foreach (var load in dropLoads)
        {
            if (!quote.ChargePerLoad.TryGetValue(load.Id, out var charge))
            {
                throw new InvalidOperationException($"The price doesn't cover load {load.Number}; price the trip again.");
            }

            load.DeliveryCost = load.DeliveryCost with { Amount = charge };
        }

        RateAgreementId = quote.AgreementId;
        // Both labels allow 200 characters, so a long row name is cut to leave room for the note.
        RateLineLabel = quote.FoulTripPercent is { } percent
            ? $"{quote.LineLabel[..Math.Min(quote.LineLabel.Length, 170)]} (foul trip, {percent:0.##}%)"
            : quote.LineLabel;
        RateBaseAmount = quote.BaseAmount;
        RateFuelAmount = quote.FuelAmount;
        RateFuelBasis = quote.FuelBasis;
        RateExtraDrops = quote.ExtraDrops;
        RateExtraDropAmount = quote.ExtraDropAmount;
        RatedOn = ratedOn;
        RatedAt = ratedAt;
    }

    /// <summary>A trip not priced yet has no price to go out of date.</summary>
    private void MarkPriceOutdated()
    {
        if (RatedAt is not null)
        {
            PriceOutdatedSince = DateTime.UtcNow;
        }
    }
}
