using Trackwise.Application.Modules.Operations.CrewPlan.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class CopyCrewPlanHandlerTests
{
    private static readonly DateOnly Monday = new(2026, 9, 28);
    private static readonly DateOnly Tuesday = new(2026, 9, 29);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<CrewAssignment, Guid> planRepo = Substitute.For<ITenantRepository<CrewAssignment, Guid>>();
    private readonly ITenantRepository<CrewStatusEntry, Guid> statusRepo = Substitute.For<ITenantRepository<CrewStatusEntry, Guid>>();
    private readonly List<CrewAssignment> added = [];
    private readonly CopyCrewPlanHandler sut;

    private readonly Truck abc1267 = new() { Number = "ABC1267", Name = "ABC1267", Type = TruckType.Reefer };
    private readonly Truck abd6775 = new() { Number = "ABD6775", Name = "ABD6775", Type = TruckType.Reefer };
    private readonly Truck retired = new() { Number = "REFLEET1", Name = "REFLEET1", Type = TruckType.Reefer, IsActive = false };
    private readonly Employee driver = new() { Code = "", Email = "wilson@crew.test", FirstName = "Wilson", LastName = "Cabrales" };
    private readonly Helper helper = new() { Code = "", Name = "Rayan Baylosis" };

    public CopyCrewPlanHandlerTests()
    {
        var truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
        truckRepo.Query().Returns(new List<Truck> { abc1267, abd6775, retired }.BuildMock());
        var employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
        employeeRepo.Query().Returns(new List<Employee> { driver }.BuildMock());
        var helperRepo = Substitute.For<ITenantRepository<Helper, Guid>>();
        helperRepo.Query().Returns(new List<Helper> { helper }.BuildMock());

        tenantUow.Repository<CrewAssignment>().Returns(planRepo);
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Helper>().Returns(helperRepo);
        tenantUow.Repository<CrewStatusEntry>().Returns(statusRepo);
        GivenStatuses();
        planRepo.AddAsync(Arg.Do<CrewAssignment>(added.Add), Arg.Any<CancellationToken>());
        sut = new CopyCrewPlanHandler(tenantUow);
    }

    private static CrewAssignment Plan(Truck truck, DateOnly date, Employee? driver = null, Helper? helper = null)
    {
        var assignment = CrewAssignment.Create(date, truck.Id);
        assignment.AssignCrew(driver, helper, null);
        return assignment;
    }

    private void GivenPlan(params CrewAssignment[] rows) => planRepo.Query().Returns(rows.ToList().BuildMock());

    private void GivenStatuses(params CrewStatusEntry[] entries) => statusRepo.Query().Returns(entries.ToList().BuildMock());

    [Fact]
    public async Task Handle_UnplannedTarget_CopiesCrewStatusAndNotes()
    {
        var monday = Plan(abc1267, Monday, driver, helper);
        monday.UnitStatus = TruckDayStatus.Buffer;
        monday.Notes = "Standby for HFI";
        GivenPlan(monday);

        var result = await sut.Handle(new CopyCrewPlanCommand { FromDate = Monday, ToDate = Tuesday }, CancellationToken.None);

        Assert.Equal(1, result.Value);
        var copy = Assert.Single(added);
        Assert.Equal((Tuesday, abc1267.Id, driver.Id, helper.Id), (copy.Date, copy.TruckId, copy.DriverId, copy.HelperId));
        Assert.Equal((TruckDayStatus.Buffer, "Standby for HFI"), (copy.UnitStatus, copy.Notes));
    }

    [Fact]
    public async Task Handle_TruckAlreadyPlannedOnTarget_LeavesItAlone()
    {
        GivenPlan(Plan(abc1267, Monday, driver), Plan(abc1267, Tuesday));

        var result = await sut.Handle(new CopyCrewPlanCommand { FromDate = Monday, ToDate = Tuesday }, CancellationToken.None);

        Assert.Equal(0, result.Value);
        Assert.Empty(added);
    }

    [Fact]
    public async Task Handle_PersonAlreadyOnAnotherTruckOnTarget_DropsThemFromTheCopy()
    {
        GivenPlan(Plan(abc1267, Monday, driver, helper), Plan(abd6775, Tuesday, driver));

        await sut.Handle(new CopyCrewPlanCommand { FromDate = Monday, ToDate = Tuesday }, CancellationToken.None);

        var copy = Assert.Single(added);
        Assert.Null(copy.DriverId);
        Assert.Equal(helper.Id, copy.HelperId);
    }

    [Fact]
    public async Task Handle_RetiredTruck_NotCopied()
    {
        GivenPlan(Plan(retired, Monday, driver));

        var result = await sut.Handle(new CopyCrewPlanCommand { FromDate = Monday, ToDate = Tuesday }, CancellationToken.None);

        Assert.Equal(0, result.Value);
        Assert.Empty(added);
    }

    [Fact]
    public async Task Handle_PersonAbsentOnTarget_DropsThemFromTheCopy()
    {
        GivenPlan(Plan(abc1267, Monday, driver, helper));
        var absent = CrewStatusEntry.ForHelper(Tuesday, helper.Id);
        absent.Status = CrewDayStatus.Absent;
        var reserve = CrewStatusEntry.ForDriver(Tuesday, driver.Id);
        reserve.Status = CrewDayStatus.Reserve;
        GivenStatuses(absent, reserve);

        await sut.Handle(new CopyCrewPlanCommand { FromDate = Monday, ToDate = Tuesday }, CancellationToken.None);

        var copy = Assert.Single(added);
        Assert.Equal(driver.Id, copy.DriverId);
        Assert.Null(copy.HelperId);
    }
}
