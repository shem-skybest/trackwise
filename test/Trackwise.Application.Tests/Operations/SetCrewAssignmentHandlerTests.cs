using Trackwise.Application.Modules.Operations.CrewPlan.Commands;
using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class SetCrewAssignmentHandlerTests
{
    private static readonly DateOnly Day = new(2026, 9, 30);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<CrewAssignment, Guid> planRepo = Substitute.For<ITenantRepository<CrewAssignment, Guid>>();
    private readonly ITripCrewService crewService = Substitute.For<ITripCrewService>();
    private readonly ICrewPlanService crewPlan = Substitute.For<ICrewPlanService>();
    private readonly SetCrewAssignmentHandler sut;

    private readonly Truck truck = new() { Number = "ABC1267", Name = "ABC1267", Type = TruckType.Reefer };
    private readonly Employee driver = new() { Code = "", Email = "mark@crew.test", FirstName = "Mark", LastName = "Alvero" };

    public SetCrewAssignmentHandlerTests()
    {
        var truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
        truckRepo.GetByIdAsync(truck.Id, Arg.Any<CancellationToken>()).Returns(truck);
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<CrewAssignment>().Returns(planRepo);
        crewService.ResolveAsync(driver.Id, null, null, Arg.Any<CancellationToken>())
            .Returns(Result<TripCrew>.Ok(new TripCrew(driver, null, null)));
        crewPlan.FindConflictAsync(Arg.Any<CrewAssignment>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        crewPlan.FindUnavailableAsync(Arg.Any<DateOnly>(), Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);
        sut = new SetCrewAssignmentHandler(tenantUow, crewService, crewPlan);
    }

    private SetCrewAssignmentCommand Command(TruckDayStatus status = TruckDayStatus.Available) => new()
    {
        Date = Day,
        TruckId = truck.Id,
        UnitStatus = status,
        DriverId = driver.Id
    };

    [Fact]
    public async Task Handle_UnplannedTruck_AddsARow()
    {
        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await planRepo.Received(1).AddAsync(
            Arg.Is<CrewAssignment>(a => a.Date == Day && a.TruckId == truck.Id && a.DriverId == driver.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TruckAlreadyPlanned_UpdatesThatRow()
    {
        var existing = CrewAssignment.Create(Day, truck.Id);
        crewPlan.FindAsync(truck.Id, Day, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await sut.Handle(Command(TruckDayStatus.ForRepair), CancellationToken.None);

        Assert.Equal(existing.Id, result.Value);
        Assert.Equal((TruckDayStatus.ForRepair, driver.Id), (existing.UnitStatus, existing.DriverId));
        await planRepo.DidNotReceive().AddAsync(Arg.Any<CrewAssignment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CrewClashesWithAnotherTruck_FailsWithoutSaving()
    {
        crewPlan.FindConflictAsync(Arg.Any<CrewAssignment>(), Arg.Any<CancellationToken>())
            .Returns("Mark Alvero is already on ABD6775's crew on 2026-09-30.");

        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("ABD6775", result.Error);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DriverAbsentThatDay_FailsWithoutSaving()
    {
        crewPlan.FindUnavailableAsync(Day, Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(driver.Id)), Arg.Any<CancellationToken>())
            .Returns("Mark Alvero is absent on 2026-09-30.");

        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("absent", result.Error);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
