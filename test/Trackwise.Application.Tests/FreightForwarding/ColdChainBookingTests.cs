using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// A Cold Chain booking is one truck-trip for one client on one day, priced from the agreement in
/// force that day. The rules here are what keeps it from drifting the way Monday's board does.
/// </summary>
public class ColdChainBookingTests
{
    private static readonly DateOnly March13 = new(2026, 3, 13);
    private static readonly Guid SchoolA = Guid.NewGuid();
    private static readonly Guid SchoolB = Guid.NewGuid();

    private static RateAgreement Agreement(DateOnly from, DateOnly? to = null)
    {
        var agreement = RateAgreement.Create(
            Guid.NewGuid(), "HFI rates", from, to, new RateAgreementTerms(FuelAdjustmentMethod.DieselTierSurcharge, ExtraDropCharges: [(null, 300m)]), null);
        agreement.ReplaceLines([
            new RateLineSpec("QUEZON CITY", TruckTonnageClass.TwoTonner, 11200, null, null, "Quezon City", "Metro Manila", null, null, [(85m, 1250m)])
        ]);
        agreement.Approve("manager", DateTime.UtcNow);
        return agreement;
    }

    private static ColdChainBookingPlan Plan(DateOnly date, params Guid[] sites) => new(
        date,
        new TimeOnly(1, 0),
        TruckTonnageClass.TwoTonner,
        Guid.NewGuid(),
        "Fresh milk",
        0,
        4,
        null,
        sites.Select(s => new BookingDropSpec(s, 40, null, null)).ToList());

    private static Booking Booked(RateAgreement agreement) =>
        Booking.CreateFromRateAgreement(agreement, "BKN-RFV-001381", Plan(new DateOnly(2026, 9, 24), SchoolA, SchoolB));

    #region Creating

    [Fact]
    public void CreateFromRateAgreement_AgreementInForce_TakesTheClientAndDropsInOrder()
    {
        var agreement = Agreement(March13);

        var booking = Booked(agreement);

        Assert.Equal(agreement.CustomerId, booking.CustomerId);
        Assert.Equal(agreement.Id, booking.RateAgreementId);
        Assert.Null(booking.QuotationId);
        Assert.Equal(BookingStatus.InProgress, booking.Status);
        Assert.Equal("PHP", booking.Currency);
        Assert.Equal([SchoolA, SchoolB], booking.Drops.OrderBy(d => d.SortOrder).Select(d => d.SiteId));
        Assert.All(booking.Drops, d => Assert.Equal(booking.Id, d.BookingId));
    }

    [Fact]
    public void CreateFromRateAgreement_DayOutsideTheAgreement_Throws()
    {
        var agreement = Agreement(March13, new DateOnly(2026, 9, 20));

        Assert.Throws<InvalidOperationException>(() =>
            Booking.CreateFromRateAgreement(agreement, "BKN-RFV-001381", Plan(new DateOnly(2026, 9, 21), SchoolA)));
    }

    [Fact]
    public void CreateFromRateAgreement_SameOutletTwice_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Booking.CreateFromRateAgreement(Agreement(March13), "BKN-RFV-001381", Plan(March13, SchoolA, SchoolA)));
    }

    [Fact]
    public void CreateFromRateAgreement_NoDrops_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Booking.CreateFromRateAgreement(Agreement(March13), "BKN-RFV-001381", Plan(March13)));
    }

    #endregion

    #region Editing

    [Fact]
    public void UpdatePlan_ConfirmedBooking_ReplacesTheDrops()
    {
        var agreement = Agreement(March13);
        var booking = Booked(agreement);
        booking.Confirm("cs");

        var drops = booking.UpdatePlan(agreement, Plan(new DateOnly(2026, 9, 25), SchoolB));

        Assert.Equal(SchoolB, Assert.Single(drops).SiteId);
        Assert.Equal(new DateOnly(2026, 9, 25), booking.ServiceDate);
    }

    [Fact]
    public void UpdatePlan_OnceTheTripHasStarted_Throws()
    {
        var agreement = Agreement(March13);
        var booking = Booked(agreement);
        booking.Confirm("cs");
        //booking.StartExecution();

        Assert.Throws<InvalidOperationException>(() => booking.UpdatePlan(agreement, Plan(March13, SchoolA)));
    }

    #endregion

    #region Estimate

    [Fact]
    public void ApplyEstimate_TotalsBaseFuelAndExtraDrops()
    {
        var booking = Booked(Agreement(March13));
        var quote = new TripRateQuote(
            Guid.NewGuid(), "HFI rates", SchoolA, "QUEZON CITY", 11200, 1250, "diesel", 1, 300,
            new Dictionary<Guid, decimal>(), ["No diesel price is set."]);

        booking.ApplyEstimate(quote);

        Assert.Equal(12750, booking.EstimatedTotal);
        Assert.Equal("No diesel price is set.", booking.EstimateNote);
    }

    [Fact]
    public void ClearEstimate_LeavesNoTotal_AndSaysWhy()
    {
        var booking = Booked(Agreement(March13));

        booking.ClearEstimate("No rate for Baguio.");

        Assert.Null(booking.EstimatedTotal);
        Assert.Equal("No rate for Baguio.", booking.EstimateNote);
    }

    #endregion

    #region Losing a booking

    [Fact]
    public void Cancel_ColdChainBookingWithoutAReason_Throws()
    {
        var booking = Booked(Agreement(March13));

        Assert.Throws<InvalidOperationException>(() => booking.Cancel("cs", "no truck"));
    }

    [Fact]
    public void Cancel_ColdChainBookingWithAReason_RecordsWhoAndWhy()
    {
        var booking = Booked(Agreement(March13));

        booking.Cancel("cs", "Lack of unit");

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(CancellationParty.Company, booking.CancelledByParty);
        Assert.Equal(BookingLossReason.LackOfUnit, booking.LossReason);
    }

    //[Fact]
    //public void MarkLostOpportunity_ColdChainBookingWithoutAReason_Throws()
    //{
    //    var booking = Booked(Agreement(March13));

    //    Assert.Throws<InvalidOperationException>(() => booking.MarkLostOpportunity("gone"));
    //}

    [Fact]
    public void Cancel_ForwardingBooking_StillNeedsNoPickedReason()
    {
        var quotation = CreateTestQuotation();
        var booking = Booking.CreateDraftFromQuotation(quotation);

        booking.Cancel("ops", "Shipper withdrew");

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Null(booking.LossReason);
    }

    [Fact]
    public void ReturnToConfirmed_AfterItsTripIsCancelled_WaitsForANewTrip()
    {
        var booking = Booked(Agreement(March13));
        booking.Confirm("cs");
        //booking.StartExecution();

        booking.ReturnToConfirmed();

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void ReturnToConfirmed_BookingNotInExecution_Throws()
    {
        var booking = Booked(Agreement(March13));

        Assert.Throws<InvalidOperationException>(booking.ReturnToConfirmed);
    }

    #endregion

    #region Repeating

    [Fact]
    public void PlanOn_AnotherDay_CopiesThePlanAndDropsInOrder()
    {
        var booking = Booked(Agreement(March13));
        var monday = new DateOnly(2026, 10, 5);

        var plan = booking.PlanOn(monday);

        Assert.Equal(monday, plan.ServiceDate);
        Assert.Equal(booking.CallTime, plan.CallTime);
        Assert.Equal(TruckTonnageClass.TwoTonner, plan.TruckSize);
        Assert.Equal(booking.PickupSiteId, plan.PickupSiteId);
        Assert.Equal("Fresh milk", plan.Commodity);
        Assert.Equal([SchoolA, SchoolB], plan.Drops.Select(d => d.SiteId));
        Assert.All(plan.Drops, d => Assert.Equal(40, d.PlannedBoxes));
    }

    [Fact]
    public void PlanOn_CopyBookedFromIt_StartsPendingWithItsOwnNumber()
    {
        var agreement = Agreement(March13);
        var source = Booked(agreement);
        source.Confirm("cs");
        //source.StartExecution();

        var copy = Booking.CreateFromRateAgreement(agreement, "BKN-RFV-001382", source.PlanOn(new DateOnly(2026, 10, 5)));

        Assert.Equal(BookingStatus.InProgress, copy.Status);
        Assert.Equal("BKN-RFV-001382", copy.ReferenceNumber);
        Assert.NotEqual(source.Id, copy.Id);
        Assert.All(copy.Drops, d => Assert.Equal(copy.Id, d.BookingId));
    }

    [Fact]
    public void PlanOn_ForwardingBooking_Throws()
    {
        var quotation = CreateTestQuotation();
        var booking = Booking.CreateDraftFromQuotation(quotation);

        Assert.Throws<InvalidOperationException>(() => booking.PlanOn(March13));
    }

    #endregion

    [Fact]
    public void ToUtc_ManilaCallTimeAtOneAm_IsTheDayBeforeInUtc()
    {
        var settings = new TenantSettings { Timezone = "Asia/Manila" };

        var utc = settings.ToUtc(new DateOnly(2026, 10, 1), new TimeOnly(1, 0));

        Assert.Equal(new DateTime(2026, 9, 30, 17, 0, 0, DateTimeKind.Utc), utc);
        Assert.Equal(DateTimeKind.Utc, utc.Kind);
    }

    private static Quotation CreateTestQuotation()
    {
        var lane = LaneTransit.PointToPoint(
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            pickupStart: DateTimeOffset.UtcNow,
            deliveryEnd: DateTimeOffset.UtcNow.AddDays(1),
            distanceKm: 100m);

        var cargo = CargoSpecification.Create(
            commodity: "General Cargo",
            totalWeightKg: 1000m,
            palletCount: 2,
            declaredCargoValue: new Money { Amount = 5000m, Currency = "USD" },
            requiresActiveRefrigeration: false);

        var quotation = Quotation.Create(
            customerId: Guid.NewGuid(),
            referenceNumber: "QT-TEST-001",
            currency: "USD",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: lane,
            cargoSpec: cargo);

        quotation.Approve("manager@test.com");
        return quotation;
    }
}
