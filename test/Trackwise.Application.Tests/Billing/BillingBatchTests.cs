using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Billing;

/// <summary>
/// A batch is one client's period handed to Accounting. What it guards: a trip is billed on its
/// current price or not at all, the charges freeze when it is locked, and every step back needs a reason.
/// </summary>
public class BillingBatchTests
{
    private static readonly BillingPeriod Sept1To15 = new(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 15));
    private static readonly DateTime Now = new(2026, 9, 16, 9, 0, 0, DateTimeKind.Utc);

    private static BillingBatch Batch() =>
        BillingBatch.Create("SLC-RFV0255-26-M", Guid.NewGuid(), Sept1To15, "PHP");

    private static BillableTrip Trip(
        decimal? amount = 6500, bool outdated = false, TripStatus status = TripStatus.Completed, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), 42, Guid.NewGuid(), status, new DateOnly(2026, 9, 3), "ABC1267", "BKN-RFV1402-26-M",
            Drops: 3, FoulTripDrops: 0, CancelledDrops: 0, PartialDrops: 0, PlannedBoxes: 44,
            RateLineLabel: "QUEZON CITY", BaseAmount: amount, FuelAmount: 0, ExtraDropAmount: 0,
            Amount: amount, IsPriceOutdated: outdated);

    private static BillingBatch LockedBatch(BillableTrip trip)
    {
        var batch = Batch();
        batch.SyncTrips([trip], includeNew: true);
        batch.Lock([trip], "ops", Now);
        return batch;
    }

    #region Trips and totals

    [Fact]
    public void SyncTrips_New_AddsLinesAndTotals()
    {
        var batch = Batch();

        var added = batch.SyncTrips([Trip(6500), Trip(4300)], includeNew: true);

        Assert.Equal(2, added.Count);
        Assert.Equal(2, batch.TripCount);
        Assert.Equal(6, batch.DropCount);
        Assert.Equal(10800, batch.VatableTotal);
        Assert.Equal(10800, batch.Total);
    }

    [Fact]
    public void SyncTrips_WithoutIncludeNew_OnlyRefreshesExistingLines()
    {
        var batch = Batch();
        var trip = Trip(6500);
        batch.SyncTrips([trip], includeNew: true);

        var added = batch.SyncTrips([trip with { Amount = 7000 }, Trip(4300)], includeNew: false);

        Assert.Empty(added);
        Assert.Equal(7000, batch.VatableTotal);
    }

    [Fact]
    public void AddCharge_NonVat_CountsApartFromService()
    {
        var batch = Batch();
        var trip = Trip(6500);
        batch.SyncTrips([trip], includeNew: true);

        batch.AddCharge(new BillingChargeSpec(BillingChargeKind.Demurrage, "Demurrage 2h", 500, IsVatable: true, trip.TripId));
        batch.AddCharge(new BillingChargeSpec(BillingChargeKind.TollsAndParking, "NLEX toll", 312, IsVatable: false));

        Assert.Equal(7000, batch.VatableTotal);
        Assert.Equal(312, batch.NonVatTotal);
        Assert.Equal(7312, batch.Total);
    }

    [Fact]
    public void AddCharge_ForATripNotOnTheBatch_Throws()
    {
        var batch = Batch();

        Assert.Throws<InvalidOperationException>(() =>
            batch.AddCharge(new BillingChargeSpec(BillingChargeKind.Backload, "Backload", 3250, true, Guid.NewGuid())));
    }

    [Fact]
    public void RemoveTrip_TakesItsChargesWithIt()
    {
        var batch = Batch();
        var trip = Trip(6500);
        batch.SyncTrips([trip, Trip(4300)], includeNew: true);
        batch.AddCharge(new BillingChargeSpec(BillingChargeKind.Demurrage, "Demurrage", 500, true, trip.TripId));

        var (line, charges) = batch.RemoveTrip(trip.TripId);

        Assert.Equal(trip.TripId, line.TripId);
        Assert.Single(charges);
        Assert.Empty(batch.Charges);
        Assert.Equal(4300, batch.Total);
    }

    #endregion

    #region Locking

    [Fact]
    public void Lock_EveryTripPricedAndCompleted_Locks()
    {
        var batch = LockedBatch(Trip());

        Assert.Equal(BillingBatchStatus.Locked, batch.Status);
        Assert.Equal("ops", batch.LockedBy);
    }

    [Theory]
    [InlineData(null, false, TripStatus.Completed, "isn't priced")]
    [InlineData(6500d, true, TripStatus.Completed, "price it again")]
    [InlineData(6500d, false, TripStatus.InTransit, "isn't completed")]
    public void Lock_TripNotBillable_ThrowsNamingTheTrip(double? amount, bool outdated, TripStatus status, string problem)
    {
        var batch = Batch();
        var trip = Trip((decimal?)amount, outdated, status);
        batch.SyncTrips([trip], includeNew: true);

        var ex = Assert.Throws<InvalidOperationException>(() => batch.Lock([trip], "ops", Now));

        Assert.Contains("Trip 42", ex.Message);
        Assert.Contains(problem, ex.Message);
        Assert.Equal(BillingBatchStatus.Draft, batch.Status);
    }

    [Fact]
    public void Lock_UsesTheTripsPriceAtLockTime()
    {
        var batch = Batch();
        var trip = Trip(6500);
        batch.SyncTrips([trip], includeNew: true);

        batch.Lock([trip with { Amount = 7200 }], "ops", Now);

        Assert.Equal(7200, batch.VatableTotal);
    }

    [Fact]
    public void Lock_NoTrips_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Batch().Lock([], "ops", Now));
    }

    [Fact]
    public void Locked_CantChange()
    {
        var trip = Trip();
        var batch = LockedBatch(trip);

        Assert.Throws<InvalidOperationException>(() => batch.SyncTrips([trip], includeNew: true));
        Assert.Throws<InvalidOperationException>(() => batch.RemoveTrip(trip.TripId));
        Assert.Throws<InvalidOperationException>(() =>
            batch.AddCharge(new BillingChargeSpec(BillingChargeKind.Other, "Extra", 100, true)));
    }

    #endregion

    #region To Accounting and back

    [Fact]
    public void ForwardThenMarkBilled_RecordsTheInvoice()
    {
        var batch = LockedBatch(Trip());

        batch.Forward(new DateOnly(2026, 9, 16), "ops");
        batch.MarkBilled("BI4907 / BS7259", new DateOnly(2026, 9, 18), "accounting");

        Assert.Equal(BillingBatchStatus.Billed, batch.Status);
        Assert.Equal("BI4907 / BS7259", batch.InvoiceNumber);
    }

    [Fact]
    public void Forward_Draft_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Batch().Forward(new DateOnly(2026, 9, 16), "ops"));
    }

    [Fact]
    public void MarkBilled_NotForwarded_Throws()
    {
        var batch = LockedBatch(Trip());

        Assert.Throws<InvalidOperationException>(() => batch.MarkBilled("BI4907", new DateOnly(2026, 9, 18), "accounting"));
    }

    [Fact]
    public void Unlock_Forwarded_BackToDraftWithTheReason()
    {
        var batch = LockedBatch(Trip());
        batch.Forward(new DateOnly(2026, 9, 16), "ops");

        batch.Unlock("Demurrage missing", "ops", Now);

        Assert.Equal(BillingBatchStatus.Draft, batch.Status);
        Assert.Equal("Demurrage missing", batch.UnlockReason);
        Assert.Null(batch.ForwardedOn);
        Assert.Null(batch.LockedAt);
    }

    [Fact]
    public void Unlock_WithoutReason_Throws()
    {
        var batch = LockedBatch(Trip());

        Assert.Throws<InvalidOperationException>(() => batch.Unlock(" ", "ops", Now));
    }

    [Fact]
    public void Unlock_Billed_Throws()
    {
        var batch = LockedBatch(Trip());
        batch.Forward(new DateOnly(2026, 9, 16), "ops");
        batch.MarkBilled("BI4907", new DateOnly(2026, 9, 18), "accounting");

        Assert.Throws<InvalidOperationException>(() => batch.Unlock("Wrong rate", "ops", Now));
    }

    [Fact]
    public void Cancel_FreesTheTripsAndKeepsTheReference()
    {
        var batch = Batch();
        batch.SyncTrips([Trip(), Trip()], includeNew: true);
        batch.AddCharge(new BillingChargeSpec(BillingChargeKind.Other, "Extra", 100, true));

        var (trips, charges) = batch.Cancel("Client billed directly", "ops", Now);

        Assert.Equal(BillingBatchStatus.Cancelled, batch.Status);
        Assert.Equal(2, trips.Count);
        Assert.Single(charges);
        Assert.Empty(batch.Trips);
        Assert.Equal(0, batch.Total);
        Assert.Equal("SLC-RFV0255-26-M", batch.ReferenceNumber);
    }

    #endregion
}
