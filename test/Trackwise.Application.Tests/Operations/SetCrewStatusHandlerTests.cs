using Trackwise.Application.Modules.Operations.CrewPlan.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class SetCrewStatusHandlerTests
{
    private static readonly DateOnly Day = new(2026, 9, 30);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<CrewStatusEntry, Guid> statusRepo = Substitute.For<ITenantRepository<CrewStatusEntry, Guid>>();
    private readonly ITenantRepository<CrewAssignment, Guid> planRepo = Substitute.For<ITenantRepository<CrewAssignment, Guid>>();
    private readonly List<CrewStatusEntry> added = [];
    private readonly SetCrewStatusHandler sut;

    private readonly Truck abc1267 = new() { Number = "ABC1267", Name = "ABC1267", Type = TruckType.Reefer };
    private readonly Employee driver = new() { Code = "", Email = "andres@crew.test", FirstName = "Andres", LastName = "Reyes" };
    private readonly Helper helper = new() { Code = "", Name = "Benito Ernesto" };
    private readonly Helper secondHelper = new() { Code = "", Name = "Rayan Baylosis" };

    public SetCrewStatusHandlerTests()
    {
        var employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
        employeeRepo.GetByIdAsync(driver.Id, Arg.Any<CancellationToken>()).Returns(driver);
        var helperRepo = Substitute.For<ITenantRepository<Helper, Guid>>();
        helperRepo.GetByIdAsync(helper.Id, Arg.Any<CancellationToken>()).Returns(helper);
        var truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
        truckRepo.Query().Returns(new List<Truck> { abc1267 }.BuildMock());

        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Helper>().Returns(helperRepo);
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<CrewStatusEntry>().Returns(statusRepo);
        tenantUow.Repository<CrewAssignment>().Returns(planRepo);
        statusRepo.AddAsync(Arg.Do<CrewStatusEntry>(added.Add), Arg.Any<CancellationToken>());
        GivenStatuses();
        GivenPlan();
        sut = new SetCrewStatusHandler(tenantUow);
    }

    private void GivenStatuses(params CrewStatusEntry[] entries) => statusRepo.Query().Returns(entries.ToList().BuildMock());

    private void GivenPlan(params CrewAssignment[] rows) => planRepo.Query().Returns(rows.ToList().BuildMock());

    [Fact]
    public async Task Handle_LeaveOverSeveralDays_AddsAnEntryPerDay()
    {
        var result = await sut.Handle(new SetCrewStatusCommand
        {
            PersonId = helper.Id, Date = Day, UntilDate = Day.AddDays(2), Status = CrewDayStatus.OnLeave, Notes = "Vacation leave"
        }, CancellationToken.None);

        Assert.Equal(3, result.Value!.Days);
        Assert.Equal([Day, Day.AddDays(1), Day.AddDays(2)], added.Select(e => e.Date));
        Assert.All(added, e => Assert.Equal((helper.Id, (Guid?)null, CrewDayStatus.OnLeave, "Vacation leave"),
            (e.HelperId!.Value, e.EmployeeId, e.Status, e.Notes)));
    }

    [Fact]
    public async Task Handle_DayAlreadyHasAnEntry_UpdatesIt()
    {
        var existing = CrewStatusEntry.ForDriver(Day, driver.Id);
        existing.Status = CrewDayStatus.Reserve;
        GivenStatuses(existing);

        await sut.Handle(new SetCrewStatusCommand { PersonId = driver.Id, Date = Day, Status = CrewDayStatus.Training }, CancellationToken.None);

        Assert.Equal(CrewDayStatus.Training, existing.Status);
        Assert.Empty(added);
    }

    [Fact]
    public async Task Handle_BackToAvailableWithoutNotes_RemovesTheEntry()
    {
        var existing = CrewStatusEntry.ForDriver(Day, driver.Id);
        existing.Status = CrewDayStatus.Absent;
        GivenStatuses(existing);

        await sut.Handle(new SetCrewStatusCommand { PersonId = driver.Id, Date = Day, Status = CrewDayStatus.Available }, CancellationToken.None);

        statusRepo.Received(1).Delete(existing);
        Assert.Empty(added);
    }

    [Fact]
    public async Task Handle_PlannedDriverMarkedAbsent_TakesThemOffTheTruck()
    {
        var plan = CrewAssignment.Create(Day, abc1267.Id);
        plan.AssignCrew(driver, null, null);
        GivenPlan(plan);

        var result = await sut.Handle(new SetCrewStatusCommand { PersonId = driver.Id, Date = Day, Status = CrewDayStatus.Absent }, CancellationToken.None);

        Assert.Equal(["ABC1267 on 2026-09-30"], result.Value!.RemovedFrom);
        Assert.Null(plan.DriverId);
        Assert.Equal(TruckDayStatus.Understaffed, plan.UnitStatus);
    }

    [Fact]
    public async Task Handle_FirstHelperMarkedAbsent_SecondHelperMovesUp()
    {
        var plan = CrewAssignment.Create(Day, abc1267.Id);
        plan.AssignCrew(driver, helper, secondHelper);
        plan.UnitStatus = TruckDayStatus.Buffer;
        GivenPlan(plan);

        await sut.Handle(new SetCrewStatusCommand { PersonId = helper.Id, Date = Day, Status = CrewDayStatus.Suspended }, CancellationToken.None);

        Assert.Equal((driver.Id, secondHelper.Id, (Guid?)null), (plan.DriverId!.Value, plan.HelperId!.Value, plan.SecondHelperId));
        Assert.Equal(TruckDayStatus.Buffer, plan.UnitStatus);
    }

    [Fact]
    public async Task Handle_ReserveDoesNotTouchThePlan()
    {
        var plan = CrewAssignment.Create(Day, abc1267.Id);
        plan.AssignCrew(driver, null, null);
        GivenPlan(plan);

        var result = await sut.Handle(new SetCrewStatusCommand { PersonId = driver.Id, Date = Day, Status = CrewDayStatus.Reserve }, CancellationToken.None);

        Assert.Empty(result.Value!.RemovedFrom);
        Assert.Equal(driver.Id, plan.DriverId);
    }

    [Fact]
    public async Task Handle_UnknownPerson_Fails()
    {
        var result = await sut.Handle(new SetCrewStatusCommand { PersonId = Guid.NewGuid(), Date = Day, Status = CrewDayStatus.Absent }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
