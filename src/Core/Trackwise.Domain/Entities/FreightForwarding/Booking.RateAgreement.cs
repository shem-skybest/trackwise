using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// The Cold Chain side of a booking: one truck-trip for one client on one day, priced from the
/// client's rate agreement rather than a quotation. What Cold Chain's booking board holds today.
/// </summary>
public partial class Booking
{
    /// <summary>The agreement in force on <see cref="ServiceDate"/> when the booking was last saved.</summary>
    public Guid? RateAgreementId { get; private set; }

    /// <summary>
    /// Staff-facing reference from the shared BKN-RFV series, e.g. "BKN-RFV1381-26-M". Null while the
    /// numbers are typed in from nSFS and this one hasn't been yet.
    /// </summary>
    public string? ReferenceNumber { get; private set; }

    /// <summary>The day the trip runs, in the tenant's local calendar.</summary>
    public DateOnly? ServiceDate { get; private set; }

    /// <summary>When the truck is due at the pickup warehouse, local time.</summary>
    public TimeOnly? CallTime { get; private set; }

    public TruckTonnageClass? TruckSize { get; private set; }

    /// <summary>The warehouse the truck loads at (a <see cref="Location"/>).</summary>
    public Guid? PickupSiteId { get; private set; }

    public string? Commodity { get; private set; }

    public decimal? TemperatureMinC { get; private set; }
    public decimal? TemperatureMaxC { get; private set; }

    public virtual List<BookingDrop> Drops { get; private set; } = [];

    /// <summary>
    /// The estimate from the rate agreement when the booking was saved. The trip's own price, set when
    /// it is priced, is what gets billed.
    /// </summary>
    public decimal? EstimatedBaseAmount { get; private set; }

    public decimal? EstimatedFuelAmount { get; private set; }
    public decimal? EstimatedExtraDropAmount { get; private set; }

    /// <summary>Why there is no estimate, or what it is missing (e.g. no diesel price set).</summary>
    public string? EstimateNote { get; private set; }

    public CancellationParty? CancelledByParty { get; private set; }
    public BookingLossReason? LossReason { get; private set; }

    public bool IsFromRateAgreement => RateAgreementId.HasValue;

    public decimal? EstimatedTotal => EstimatedBaseAmount is { } baseAmount
        ? baseAmount + (EstimatedFuelAmount ?? 0) + (EstimatedExtraDropAmount ?? 0)
        : null;

    public static Booking CreateFromRateAgreement(
        RateAgreement agreement, string? referenceNumber, ColdChainBookingPlan plan)
    {
        var booking = new Booking
        {
            CustomerId = agreement.CustomerId,
            ReferenceNumber = referenceNumber,
            Status = BookingStatus.Draft
        };

        booking.SetPlan(agreement, plan);
        //booking.DomainEvents.Add(new BookingCreated(booking.Id, null, agreement.CustomerId));
        return booking;
    }

    /// <summary>
    /// Replaces the plan of a booking that hasn't started. Returns the new drops so the caller can
    /// register them with the repository - a drop reaching the tracker only through this collection
    /// would be saved as an update to a row that does not exist.
    /// </summary>
    public IReadOnlyList<BookingDrop> UpdatePlan(RateAgreement agreement, ColdChainBookingPlan plan)
    {
        if (!IsFromRateAgreement)
        {
            throw new InvalidOperationException("Only a booking made from a rate agreement has a trip plan.");
        }

        if (Status is not (BookingStatus.Draft or BookingStatus.Confirmed or BookingStatus.Hold))
        {
            throw new InvalidOperationException("Only a pending, confirmed or on-hold booking can be changed.");
        }

        SetPlan(agreement, plan);
        return Drops;
    }

    /// <summary>
    /// This booking's trip plan moved to another day - what a standing order repeats. Notes come
    /// along; the status, estimate and trips don't.
    /// </summary>
    public ColdChainBookingPlan PlanOn(DateOnly serviceDate)
    {
        if (!IsFromRateAgreement || TruckSize is not { } truckSize)
        {
            throw new InvalidOperationException("Only a booking made from a rate agreement can be repeated.");
        }

        return new ColdChainBookingPlan(
            serviceDate,
            CallTime,
            truckSize,
            PickupSiteId,
            Commodity,
            TemperatureMinC,
            TemperatureMaxC,
            Notes,
            [.. Drops.OrderBy(d => d.SortOrder)
                .Select(d => new BookingDropSpec(d.SiteId, d.PlannedBoxes, d.PlannedBags, d.Notes))]);
    }

    /// <summary>
    /// Records the number nSFS gave the booking, which often arrives after the truck has loaded, so
    /// it can be set at any stage. The caller checks no other booking has it.
    /// </summary>
    public void SetReferenceNumber(string referenceNumber)
    {
        if (!IsFromRateAgreement)
        {
            throw new InvalidOperationException("Only a Cold Chain booking has a BKN-RFV number.");
        }

        ReferenceNumber = referenceNumber;
    }

    public void ApplyEstimate(TripRateQuote quote)
    {
        EstimatedBaseAmount = quote.BaseAmount;
        EstimatedFuelAmount = quote.FuelAmount;
        EstimatedExtraDropAmount = quote.ExtraDropAmount;
        EstimateNote = quote.Warnings.Count > 0 ? string.Join(" ", quote.Warnings) : null;
    }

    public void ClearEstimate(string reason)
    {
        EstimatedBaseAmount = null;
        EstimatedFuelAmount = null;
        EstimatedExtraDropAmount = null;
        EstimateNote = reason;
    }

    /// <summary>Its trip was cancelled, so the booking waits for another one.</summary>
    public void ReturnToConfirmed()
    {
        if (Status is not BookingStatus.InProgress)
        {
            throw new InvalidOperationException("Only a booking in execution can go back to confirmed.");
        }

        Status = BookingStatus.Confirmed;
    }

    private void SetPlan(RateAgreement agreement, ColdChainBookingPlan plan)
    {
        if (agreement.CustomerId != CustomerId)
        {
            throw new InvalidOperationException("The rate agreement belongs to another client.");
        }

        if (!agreement.IsInForceOn(plan.ServiceDate))
        {
            throw new InvalidOperationException(
                $"'{agreement.Name}' isn't in force on {plan.ServiceDate:yyyy-MM-dd}.");
        }

        if (plan.Drops.Count == 0)
        {
            throw new InvalidOperationException("A booking needs at least one drop.");
        }

        if (plan.Drops.Select(d => d.SiteId).Distinct().Count() != plan.Drops.Count)
        {
            throw new InvalidOperationException("Each delivery location can only be one drop on a booking.");
        }

        if (plan.TemperatureMinC > plan.TemperatureMaxC)
        {
            throw new InvalidOperationException("The lowest temperature can't be above the highest.");
        }

        RateAgreementId = agreement.Id;
        Currency = agreement.Currency;
        ServiceDate = plan.ServiceDate;
        CallTime = plan.CallTime;
        TruckSize = plan.TruckSize;
        PickupSiteId = plan.PickupSiteId;
        Commodity = string.IsNullOrWhiteSpace(plan.Commodity) ? null : plan.Commodity.Trim();
        TemperatureMinC = plan.TemperatureMinC;
        TemperatureMaxC = plan.TemperatureMaxC;
        Notes = string.IsNullOrWhiteSpace(plan.Notes) ? null : plan.Notes.Trim();

        Drops.Clear();
        var order = 0;
        foreach (var spec in plan.Drops)
        {
            Drops.Add(BookingDrop.Create(Id, order++, spec));
        }
    }

    private void EnsureLossReason(BookingLossReason? lossReason, string outcome)
    {
        if (IsFromRateAgreement && lossReason is null)
        {
            throw new InvalidOperationException($"Pick why the booking was {outcome}.");
        }
    }
}
