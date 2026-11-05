using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class TripCorrectionTests
{
    private const string Reason = "typo, crew wrote 2329";

    private static readonly DateTime Start =
        new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(-1);

    private static Load NewLoad(string name) => new()
    {
        Name = name,
        Type = LoadType.GeneralFreight,
        Customer = null!,
        OriginAddress = new Address
        {
            StreetAddress = "1 Origin",
            CityOrMunicipality = "City",
            Province = "ST",
            Region = "",
            Zipcode = "00000",
            Country = "PH"
        },
        OriginLocation = new GeoPoint(0, 0),
        DestinationAddress = new Address
        {
            StreetAddress = "1 Dest",
            CityOrMunicipality = "City",
            Province = "ST",
            Region = "",
            Zipcode = "00000",
            Country = "PH"
        },
        DestinationLocation = new GeoPoint(1, 1),
        DeliveryCost = Money.Zero("PHP"),
        PlannedBoxes = 40
    };

    /// <summary>Pickup 01:00-02:00, drop 03:00-04:00 delivered, garage 00:30 and 05:00.</summary>
    private static (Trip trip, Load load, TripStop pickup, TripStop drop) CompletedTrip()
    {
        var load = NewLoad("Load 1");
        var trip = Trip.Create("Trip 1", truck: null, loads: [load]);
        var pickup = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var drop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);
        trip.Dispatch();
        trip.MarkStopArrived(pickup.Id, arrivedAt: Start.AddHours(1));
        trip.MarkStopDeparted(pickup.Id, new StopDeparture(null, DepartedAt: Start.AddHours(2)));
        trip.MarkStopArrived(drop.Id, temperatureAtArrival: -18m, arrivedAt: Start.AddHours(3));
        trip.MarkStopDeparted(drop.Id, new StopDeparture(LoadStatus.Delivered, DepartedAt: Start.AddHours(4)));
        trip.RecordGarageTimes(Start.AddMinutes(30), Start.AddHours(5));
        return (trip, load, pickup, drop);
    }

    private static StopCorrection AsRecorded(TripStop stop) => new(
        stop.ArrivedAt!.Value,
        stop.TemperatureAtArrival,
        stop.DepartedAt,
        stop.TemperatureAtDeparture,
        stop.HandlingStartedAt,
        stop.HandlingFinishedAt,
        stop.Type == TripStopType.DropOff ? stop.Load.Status : null,
        stop.Load.ReturnedBoxes,
        stop.Load.ReturnedBags,
        stop.Load.ReturnReason,
        stop.Load.OutcomeNote);

    #region Stop corrections

    [Fact]
    public void CorrectStop_ArrivalTime_ChangesStopAndLoadAndLogsOneRow()
    {
        var (trip, load, _, drop) = CompletedTrip();
        var corrected = Start.AddHours(3).AddMinutes(29);

        var changes = trip.CorrectStop(drop.Id, AsRecorded(drop) with { ArrivedAt = corrected }, Reason, "office");

        var change = Assert.Single(changes);
        Assert.Equal(TripCorrectionField.ArrivedAt, change.Field);
        Assert.Equal(drop.Id, change.StopId);
        Assert.Equal(Reason, change.Reason);
        Assert.Equal("office", change.CorrectedBy);
        Assert.Equal(corrected, drop.ArrivedAt);
        Assert.Equal(corrected, load.DeliveredAt);
    }

    [Fact]
    public void CorrectStop_KeepsWhoFirstRecordedTheEvent()
    {
        var (trip, _, _, drop) = CompletedTrip();
        var recordedAt = drop.ArrivalRecordedAt;

        trip.CorrectStop(drop.Id, AsRecorded(drop) with { ArrivedAt = Start.AddHours(3).AddMinutes(5) }, Reason, "office");

        Assert.Equal(recordedAt, drop.ArrivalRecordedAt);
    }

    [Fact]
    public void CorrectStop_NothingChanged_Throws()
    {
        var (trip, _, _, drop) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() => trip.CorrectStop(drop.Id, AsRecorded(drop), Reason, null));
    }

    [Fact]
    public void CorrectStop_SameMinuteWithoutSeconds_CountsAsUnchanged()
    {
        var (trip, _, _, drop) = CompletedTrip();
        var withSeconds = Start.AddHours(3).AddMinutes(10).AddSeconds(42);
        trip.CorrectStop(drop.Id, AsRecorded(drop) with { ArrivedAt = withSeconds }, Reason, null);

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop.Id, AsRecorded(drop) with { ArrivedAt = Start.AddHours(3).AddMinutes(10) }, Reason, null));
        Assert.Equal(withSeconds, drop.ArrivedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CorrectStop_NoReason_Throws(string reason)
    {
        var (trip, _, _, drop) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop.Id, AsRecorded(drop) with { TemperatureAtArrival = -17m }, reason, null));
    }

    [Fact]
    public void CorrectStop_DepartureBeforeArrival_Throws()
    {
        var (trip, _, _, drop) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop.Id, AsRecorded(drop) with { DepartedAt = Start.AddHours(2).AddMinutes(30) }, Reason, null));
    }

    [Fact]
    public void CorrectStop_ArrivalBeforeGarageDeparture_Throws()
    {
        var (trip, _, pickup, _) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(pickup.Id, AsRecorded(pickup) with { ArrivedAt = Start }, Reason, null));
    }

    [Fact]
    public void CorrectStop_DepartureAfterGarageArrival_Throws()
    {
        var (trip, _, _, drop) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop.Id, AsRecorded(drop) with { DepartedAt = Start.AddHours(6) }, Reason, null));
    }

    [Fact]
    public void CorrectStop_ClearingRecordedDeparture_Throws()
    {
        var (trip, _, _, drop) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop.Id, AsRecorded(drop) with { DepartedAt = null }, Reason, null));
    }

    [Fact]
    public void CorrectStop_DepartureNotYetRecorded_Throws()
    {
        var load = NewLoad("Load 1");
        var trip = Trip.Create("Trip 1", truck: null, loads: [load]);
        var pickup = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        trip.Dispatch();
        trip.MarkStopArrived(pickup.Id, arrivedAt: Start.AddHours(1));

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(pickup.Id, AsRecorded(pickup) with { DepartedAt = Start.AddHours(2) }, Reason, null));
    }

    [Fact]
    public void CorrectStop_NothingRecordedOnStop_Throws()
    {
        var load = NewLoad("Load 1");
        var trip = Trip.Create("Trip 1", truck: null, loads: [load]);
        var pickup = trip.Stops.Single(s => s.Type == TripStopType.PickUp);
        var drop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);
        trip.Dispatch();
        trip.MarkStopArrived(pickup.Id, arrivedAt: Start.AddHours(1));

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop.Id, new StopCorrection(Start.AddHours(3)), Reason, null));
    }

    [Fact]
    public void CorrectStop_LastDropDeparture_MovesCompletedAt()
    {
        var (trip, _, _, drop) = CompletedTrip();
        var corrected = Start.AddHours(4).AddMinutes(15);

        trip.CorrectStop(drop.Id, AsRecorded(drop) with { DepartedAt = corrected }, Reason, null);

        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Equal(corrected, trip.CompletedAt);
    }

    [Fact]
    public void CorrectStop_PickupOfTwoLoads_CorrectsTheWholeWarehouseVisit()
    {
        var trip = Trip.Create("Trip 2", truck: null, loads: [NewLoad("A"), NewLoad("B")]);
        var pickups = trip.Stops.Where(s => s.Type == TripStopType.PickUp).ToList();
        trip.Dispatch();
        foreach (var p in pickups)
        {
            trip.MarkStopArrived(p.Id, arrivedAt: Start.AddHours(1));
        }

        var changes = trip.CorrectStop(
            pickups[0].Id, new StopCorrection(Start.AddHours(1).AddMinutes(20)), Reason, null);

        Assert.Equal(2, changes.Count);
        Assert.All(pickups, p => Assert.Equal(Start.AddHours(1).AddMinutes(20), p.ArrivedAt));
        Assert.All(pickups, p => Assert.Equal(Start.AddHours(1).AddMinutes(20), p.Load.PickedUpAt));
    }

    [Fact]
    public void CorrectStop_DraftTrip_Throws()
    {
        var trip = Trip.Create("Trip 1", truck: null, loads: [NewLoad("Load 1")]);
        var drop = trip.Stops.Single(s => s.Type == TripStopType.DropOff);

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop.Id, new StopCorrection(Start), Reason, null));
    }

    #endregion

    #region Outcomes and the price

    [Fact]
    public void CorrectStop_DeliveredToFoulTrip_ResolvesLoadAndLogsOutcome()
    {
        var (trip, load, _, drop) = CompletedTrip();

        var changes = trip.CorrectStop(
            drop.Id,
            AsRecorded(drop) with { Outcome = LoadStatus.FoulTrip, OutcomeNote = "store closed" },
            Reason,
            null);

        Assert.Equal(LoadStatus.FoulTrip, load.Status);
        Assert.Equal(drop.DepartedAt, load.CancelledAt);
        Assert.Contains(changes, c => c.Field == TripCorrectionField.Outcome
                                      && c.OldValue == nameof(LoadStatus.Delivered)
                                      && c.NewValue == nameof(LoadStatus.FoulTrip));
        Assert.Contains(changes, c => c.Field == TripCorrectionField.OutcomeNote && c.NewValue == "store closed");
        Assert.Equal(TripStatus.Completed, trip.Status);
    }

    [Fact]
    public void CorrectStop_PartialReturn_RecordsReturnedBoxesAndReason()
    {
        var (trip, load, _, drop) = CompletedTrip();

        trip.CorrectStop(
            drop.Id,
            AsRecorded(drop) with { ReturnedBoxes = 2, ReturnReason = DropReturnReason.Expired },
            Reason,
            null);

        Assert.Equal(2, load.ReturnedBoxes);
        Assert.Equal(DropReturnReason.Expired, load.ReturnReason);
    }

    [Fact]
    public void CorrectStop_ReturnsWithoutReason_Throws()
    {
        var (trip, _, _, drop) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectStop(drop.Id, AsRecorded(drop) with { ReturnedBoxes = 2 }, Reason, null));
    }

    [Fact]
    public void CorrectStop_OutcomeChangedAfterPricing_MarksPriceOutdated()
    {
        var (trip, load, _, drop) = CompletedTrip();
        trip.ApplyRate(Quote(load), DateOnly.FromDateTime(Start), DateTime.UtcNow.AddMinutes(-1));

        trip.CorrectStop(drop.Id, AsRecorded(drop) with { Outcome = LoadStatus.FoulTrip, OutcomeNote = "x" }, Reason, null);

        Assert.True(trip.IsPriceOutdated);
    }

    [Fact]
    public void CorrectStop_TimeOnlyAfterPricing_LeavesPriceCurrent()
    {
        var (trip, load, _, drop) = CompletedTrip();
        trip.ApplyRate(Quote(load), DateOnly.FromDateTime(Start), DateTime.UtcNow.AddMinutes(-1));

        trip.CorrectStop(drop.Id, AsRecorded(drop) with { ArrivedAt = Start.AddHours(3).AddMinutes(10) }, Reason, null);

        Assert.False(trip.IsPriceOutdated);
    }

    [Fact]
    public void ApplyRate_AfterOutcomeCorrection_ClearsPriceOutdated()
    {
        var (trip, load, _, drop) = CompletedTrip();
        trip.ApplyRate(Quote(load), DateOnly.FromDateTime(Start), DateTime.UtcNow.AddMinutes(-1));
        trip.CorrectStop(drop.Id, AsRecorded(drop) with { Outcome = LoadStatus.FoulTrip, OutcomeNote = "x" }, Reason, null);

        trip.ApplyRate(Quote(load), DateOnly.FromDateTime(Start), DateTime.UtcNow.AddMinutes(1));

        Assert.False(trip.IsPriceOutdated);
    }

    private static TripRateQuote Quote(Load load) => new(
        Guid.NewGuid(), "HFI 2026", load.Id, "QUEZON CITY", 5000m, 500m, "diesel", 0, 0m,
        new Dictionary<Guid, decimal> { [load.Id] = 5500m }, []);

    #endregion

    #region Garage times

    [Fact]
    public void CorrectGarageTimes_ChangesBothAndLogsTwoRows()
    {
        var (trip, _, _, _) = CompletedTrip();

        var changes = trip.CorrectGarageTimes(Start.AddMinutes(15), Start.AddHours(5).AddMinutes(30), Reason, null);

        Assert.Equal(2, changes.Count);
        Assert.All(changes, c => Assert.Null(c.StopId));
        Assert.Equal(Start.AddMinutes(15), trip.GarageDepartedAt);
        Assert.Equal(Start.AddHours(5).AddMinutes(30), trip.GarageArrivedAt);
    }

    [Fact]
    public void CorrectGarageTimes_DepartureAfterFirstArrival_Throws()
    {
        var (trip, _, _, _) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectGarageTimes(Start.AddHours(1).AddMinutes(30), trip.GarageArrivedAt, Reason, null));
    }

    [Fact]
    public void CorrectGarageTimes_ClearingRecordedTime_Throws()
    {
        var (trip, _, _, _) = CompletedTrip();

        Assert.Throws<InvalidOperationException>(() =>
            trip.CorrectGarageTimes(null, trip.GarageArrivedAt, Reason, null));
    }

    #endregion
}
