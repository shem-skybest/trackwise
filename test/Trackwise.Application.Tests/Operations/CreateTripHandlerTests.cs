using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Application.Modules.Operations.Loads.Services;
using Trackwise.Application.Modules.Operations.Trips.Commands;
using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class CreateTripHandlerTests
{
    private static readonly DateOnly Today = new(2026, 9, 30);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Trip, Guid> tripRepo = Substitute.For<ITenantRepository<Trip, Guid>>();
    private readonly ITripCrewService crewService = Substitute.For<ITripCrewService>();
    private readonly ICrewPlanService crewPlan = Substitute.For<ICrewPlanService>();
    private readonly CreateTripHandler sut;

    private readonly Truck truck = new() { Number = "ABC1267", Name = "ABC1267", Type = TruckType.Reefer };
    private readonly Employee plannedDriver = new() { Code = "", Email = "mark@crew.test", FirstName = "Mark", LastName = "Alvero" };
    private readonly Helper plannedHelper = new() { Code = "", Name = "Rayan Baylosis" };
    private Trip? created;

    public CreateTripHandlerTests()
    {
        var truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
        truckRepo.GetByIdAsync(truck.Id, Arg.Any<CancellationToken>()).Returns(truck);
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<Trip>().Returns(tripRepo);
        tripRepo.AddAsync(Arg.Do<Trip>(t => created = t), Arg.Any<CancellationToken>());

        crewPlan.TripDay(Arg.Any<IEnumerable<Load>>()).Returns(Today);
        crewPlan.FindUnavailableAsync(Arg.Any<DateOnly>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        crewService.ResolveAsync(Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Result<TripCrew>.Ok(new TripCrew(null, null, null)));
        crewService.ResolveAsync(plannedDriver.Id, plannedHelper.Id, null, Arg.Any<CancellationToken>())
            .Returns(Result<TripCrew>.Ok(new TripCrew(plannedDriver, plannedHelper, null)));

        sut = new CreateTripHandler(
            tenantUow,
            Substitute.For<ILoadService>(),
            crewService,
            crewPlan,
            NullLogger<CreateTripHandler>.Instance);
    }

    private void GivenPlannedCrew()
    {
        var plan = CrewAssignment.Create(Today, truck.Id);
        plan.AssignCrew(plannedDriver, plannedHelper, null);
        crewPlan.FindAsync(truck.Id, Today, Arg.Any<CancellationToken>()).Returns(plan);
    }

    [Fact]
    public async Task Handle_NoCrewGivenAndTruckPlannedToday_TakesThePlannedCrew()
    {
        GivenPlannedCrew();

        var result = await sut.Handle(new CreateTripCommand { Name = "ABC1267 - HFI", TruckId = truck.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(plannedDriver.Id, created!.DriverId);
        Assert.Equal(plannedHelper.Id, created.HelperId);
    }

    [Fact]
    public async Task Handle_CrewGiven_KeepsItOverThePlan()
    {
        GivenPlannedCrew();
        var onCall = new Employee { Code = "", Email = "oncall@crew.test", FirstName = "On", LastName = "Call" };
        crewService.ResolveAsync(onCall.Id, null, null, Arg.Any<CancellationToken>())
            .Returns(Result<TripCrew>.Ok(new TripCrew(onCall, null, null)));

        await sut.Handle(
            new CreateTripCommand { Name = "ABC1267 - 2nd trip", TruckId = truck.Id, DriverId = onCall.Id },
            CancellationToken.None);

        Assert.Equal(onCall.Id, created!.DriverId);
        Assert.Null(created.HelperId);
        await crewPlan.DidNotReceive().FindAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TruckNotPlanned_FallsBackToMainDriver()
    {
        var mainDriver = new Employee { Code = "", Email = "wilson@crew.test", FirstName = "Wilson", LastName = "Cabrales" };
        truck.MainDriver = mainDriver;
        truck.MainDriverId = mainDriver.Id;

        await sut.Handle(new CreateTripCommand { Name = "ABC1267 - HFI", TruckId = truck.Id }, CancellationToken.None);

        Assert.Equal(mainDriver.Id, created!.DriverId);
    }

    [Fact]
    public async Task Handle_MainDriverAbsentThatDay_FailsWithoutCreating()
    {
        var mainDriver = new Employee { Code = "", Email = "wilson@crew.test", FirstName = "Wilson", LastName = "Cabrales" };
        truck.MainDriver = mainDriver;
        truck.MainDriverId = mainDriver.Id;
        crewPlan.FindUnavailableAsync(Today, Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(mainDriver.Id)), Arg.Any<CancellationToken>())
            .Returns("Wilson Cabrales is absent on 2026-09-30.");

        var result = await sut.Handle(new CreateTripCommand { Name = "ABC1267 - HFI", TruckId = truck.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("absent", result.Error);
        await tripRepo.DidNotReceive().AddAsync(Arg.Any<Trip>(), Arg.Any<CancellationToken>());
    }
}
