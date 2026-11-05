using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Operations.Trips.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>The ABC1267 walkthrough: one warehouse, two drops, entered from the crew's message in one go.</summary>
public class RecordTripReportHandlerTests
{
    private static readonly GeoPoint Warehouse = new(121.02, 14.55);
    private static readonly DateTime GarageOut = DateTime.UtcNow.AddHours(-12);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly RecordTripReportHandler sut;
    private readonly Load bonifacio = NewLoad("Bonifacio E/S", new GeoPoint(121.03, 14.66));
    private readonly Load apolonio = NewLoad("Apolonio E/S", new GeoPoint(121.05, 14.68));
    private readonly Trip trip;

    public RecordTripReportHandlerTests()
    {
        trip = Trip.Create("ABC1267 - HFI", truck: null, loads: [bonifacio, apolonio]);
        var tripRepo = Substitute.For<ITenantRepository<Trip, Guid>>();
        tripRepo.GetByIdAsync(trip.Id, Arg.Any<CancellationToken>()).Returns(trip);
        tenantUow.Repository<Trip>().Returns(tripRepo);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetUserName().Returns("office@skybest.test");
        sut = new RecordTripReportHandler(tenantUow, currentUser, NullLogger<RecordTripReportHandler>.Instance);
    }

    private static Load NewLoad(string name, GeoPoint drop) => new()
    {
        Name = name,
        Type = LoadType.RefrigeratedGoods,
        Customer = null!,
        OriginAddress = new Address { StreetAddress = "WH", CityOrMunicipality = "Taguig", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        OriginLocation = Warehouse,
        DestinationAddress = new Address { StreetAddress = name, CityOrMunicipality = "Quezon City", Province = "NCR", Region = "", Zipcode = "", Country = "PH" },
        DestinationLocation = drop,
        DeliveryCost = Money.Zero("PHP"),
        PlannedBoxes = 20
    };

    private TripStop PickupOf(Load load) => trip.Stops.Single(s => s.Type == TripStopType.PickUp && s.LoadId == load.Id);
    private TripStop DropOf(Load load) => trip.Stops.Single(s => s.Type == TripStopType.DropOff && s.LoadId == load.Id);

    private static TripReportStopEntry Drop(TripStop stop, DateTime arrived, DateTime departed) => new()
    {
        StopId = stop.Id, ArrivedAt = arrived, DepartedAt = departed, Outcome = LoadStatus.Delivered
    };

    private RecordTripReportCommand FullReport() => new()
    {
        TripId = trip.Id,
        GarageDepartedAt = GarageOut,
        GarageArrivedAt = GarageOut.AddHours(10),
        Stops =
        [
            new TripReportStopEntry
            {
                StopId = PickupOf(bonifacio).Id,
                ArrivedAt = GarageOut.AddHours(1),
                TemperatureAtArrival = 13,
                HandlingStartedAt = GarageOut.AddHours(5),
                HandlingFinishedAt = GarageOut.AddHours(6),
                DepartedAt = GarageOut.AddHours(6.5)
            },
            Drop(DropOf(bonifacio), GarageOut.AddHours(7), GarageOut.AddHours(7.5)),
            Drop(DropOf(apolonio), GarageOut.AddHours(8), GarageOut.AddHours(8.5))
        ]
    };

    [Fact]
    public async Task Handle_FullReport_RecordsEveryStopTheWarehouseVisitAndGarageTimes()
    {
        var result = await sut.Handle(FullReport(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.All(trip.Stops, s => Assert.NotNull(s.DepartedAt));
        Assert.Equal(PickupOf(bonifacio).ArrivedAt, PickupOf(apolonio).ArrivedAt);
        Assert.Equal(13, PickupOf(apolonio).TemperatureAtArrival);
        Assert.Equal(GarageOut.AddHours(5), PickupOf(apolonio).HandlingStartedAt);
        Assert.Equal((GarageOut, GarageOut.AddHours(10)), (trip.GarageDepartedAt!.Value, trip.GarageArrivedAt!.Value));
        Assert.Equal(TripStatus.Completed, trip.Status);
        Assert.Equal("office@skybest.test", DropOf(apolonio).ArrivalRecordedBy);
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BadTimeOnSecondDrop_NamesTheDropAndSavesNothing()
    {
        var report = FullReport();
        report.Stops[2].DepartedAt = GarageOut.AddHours(7.9);

        var result = await sut.Handle(report, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("Drop 2:", result.Error);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GarageDepartureAfterWarehouseArrival_Fails()
    {
        var report = FullReport();
        report.GarageDepartedAt = GarageOut.AddHours(2);

        var result = await sut.Handle(report, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.StartsWith("Garage:", result.Error);
    }

    [Fact]
    public async Task Handle_ArrivalAlreadyRecorded_KeepsIt()
    {
        var recorded = GarageOut.AddMinutes(50);
        trip.MarkStopArrived(PickupOf(bonifacio).Id, 12, recorded);

        var result = await sut.Handle(FullReport(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal(recorded, PickupOf(bonifacio).ArrivedAt);
        Assert.Equal(GarageOut.AddHours(1), PickupOf(apolonio).ArrivedAt);
    }

    [Fact]
    public async Task Handle_StopFromAnotherTrip_Fails()
    {
        var report = FullReport();
        report.Stops.Add(new TripReportStopEntry { StopId = Guid.NewGuid(), ArrivedAt = GarageOut });

        var result = await sut.Handle(report, CancellationToken.None);

        Assert.False(result.IsSuccess);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
