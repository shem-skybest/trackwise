using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Application.Tests.TestKit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

public class CrewPlanServiceTests
{
    private static readonly DateOnly Day = new(2026, 9, 30);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<CrewAssignment, Guid> planRepo = Substitute.For<ITenantRepository<CrewAssignment, Guid>>();
    private readonly ITenantRepository<Truck, Guid> truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
    private readonly ITenantRepository<Employee, Guid> employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
    private readonly ITenantRepository<Helper, Guid> helperRepo = Substitute.For<ITenantRepository<Helper, Guid>>();
    private readonly ITenantRepository<CrewStatusEntry, Guid> statusRepo = Substitute.For<ITenantRepository<CrewStatusEntry, Guid>>();
    private readonly CrewPlanService sut;

    private readonly Truck abc1267 = new() { Number = "ABC1267", Name = "ABC1267", Type = TruckType.Reefer };
    private readonly Truck abd6775 = new() { Number = "ABD6775", Name = "ABD6775", Type = TruckType.Reefer };
    private readonly Employee driver = new() { Code = "", Email = "mark@crew.test", FirstName = "Mark", LastName = "Alvero" };
    private readonly Helper helper = new() { Code = "", Name = "Rayan Baylosis" };

    public CrewPlanServiceTests()
    {
        tenantUow.Repository<CrewAssignment>().Returns(planRepo);
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Helper>().Returns(helperRepo);
        tenantUow.Repository<CrewStatusEntry>().Returns(statusRepo);
        truckRepo.GetByIdAsync(abd6775.Id, Arg.Any<CancellationToken>()).Returns(abd6775);
        employeeRepo.GetByIdAsync(driver.Id, Arg.Any<CancellationToken>()).Returns(driver);
        helperRepo.GetByIdAsync(helper.Id, Arg.Any<CancellationToken>()).Returns(helper);
        sut = new CrewPlanService(tenantUow);
    }

    private static CrewAssignment Plan(Truck truck, DateOnly date, Employee? driver = null, Helper? helper = null)
    {
        var assignment = CrewAssignment.Create(date, truck.Id);
        assignment.AssignCrew(driver, helper, null);
        return assignment;
    }

    private void GivenPlan(params CrewAssignment[] rows) => planRepo.Query().Returns(rows.ToList().BuildMock());

    #region Conflicts

    [Fact]
    public async Task FindConflict_DriverOnAnotherTruckSameDay_NamesDriverAndTruck()
    {
        var other = Plan(abd6775, Day, driver);
        var mine = Plan(abc1267, Day, driver);
        GivenPlan(other, mine);

        var conflict = await sut.FindConflictAsync(mine);

        Assert.Equal("Mark Alvero is already on ABD6775's crew on 2026-09-30.", conflict);
    }

    [Fact]
    public async Task FindConflict_HelperOnAnotherTruckAsSecondHelper_ReturnsConflict()
    {
        var other = CrewAssignment.Create(Day, abd6775.Id);
        other.AssignCrew(null, new Helper { Code = "", Name = "Fely Moreno" }, helper);
        var mine = Plan(abc1267, Day, helper: helper);
        GivenPlan(other, mine);

        Assert.NotNull(await sut.FindConflictAsync(mine));
    }

    [Fact]
    public async Task FindConflict_SamePersonOnAnotherDay_NoConflict()
    {
        GivenPlan(Plan(abd6775, Day.AddDays(-1), driver));

        Assert.Null(await sut.FindConflictAsync(Plan(abc1267, Day, driver)));
    }

    [Fact]
    public async Task FindConflict_SameTruckRow_NoConflict()
    {
        var mine = Plan(abc1267, Day, driver);
        GivenPlan(mine);

        Assert.Null(await sut.FindConflictAsync(mine));
    }

    #endregion

    #region Crew status

    private void GivenStatus(CrewStatusEntry entry, CrewDayStatus status)
    {
        entry.Status = status;
        statusRepo.Query().Returns(new List<CrewStatusEntry> { entry }.BuildMock());
    }

    [Theory]
    [InlineData(CrewDayStatus.Absent, "absent")]
    [InlineData(CrewDayStatus.OnLeave, "on leave")]
    [InlineData(CrewDayStatus.Suspended, "suspended")]
    public async Task FindUnavailable_HelperNotWorkingThatDay_NamesThemAndWhy(CrewDayStatus status, string why)
    {
        GivenStatus(CrewStatusEntry.ForHelper(Day, helper.Id), status);

        var message = await sut.FindUnavailableAsync(Day, [driver.Id, helper.Id]);

        Assert.Equal($"Rayan Baylosis is {why} on 2026-09-30.", message);
    }

    [Theory]
    [InlineData(CrewDayStatus.Reserve)]
    [InlineData(CrewDayStatus.Training)]
    [InlineData(CrewDayStatus.LentOut)]
    public async Task FindUnavailable_StatusThatDoesNotBlock_ReturnsNull(CrewDayStatus status)
    {
        GivenStatus(CrewStatusEntry.ForDriver(Day, driver.Id), status);

        Assert.Null(await sut.FindUnavailableAsync(Day, [driver.Id]));
    }

    [Fact]
    public async Task FindUnavailable_AbsentOnAnotherDay_ReturnsNull()
    {
        GivenStatus(CrewStatusEntry.ForDriver(Day.AddDays(-1), driver.Id), CrewDayStatus.Absent);

        Assert.Null(await sut.FindUnavailableAsync(Day, [driver.Id]));
    }

    #endregion

    #region Local date

    [Theory]
    [InlineData("2026-09-29T15:59:00Z", "2026-09-29")]
    [InlineData("2026-09-29T16:00:00Z", "2026-09-30")]
    public void LocalDate_ManilaTenant_RollsOverAtUtcFourPm(string utc, string expected)
    {
        var tenant = TestTenant.Create();
        tenant.Settings.Timezone = "Asia/Manila";
        tenantUow.GetCurrentTenant().Returns(tenant);

        var date = sut.LocalDate(DateTime.Parse(utc, null, System.Globalization.DateTimeStyles.AdjustToUniversal));

        Assert.Equal(DateOnly.Parse(expected), date);
    }

    [Fact]
    public void TripDay_LoadsWithPickupDates_IsTheEarliestPickupsLocalDay()
    {
        var tenant = TestTenant.Create();
        tenant.Settings.Timezone = "Asia/Manila";
        tenantUow.GetCurrentTenant().Returns(tenant);
        var loads = new[] { PickingUpAt(new DateTime(2026, 9, 30, 20, 0, 0, DateTimeKind.Utc)), PickingUpAt(new DateTime(2026, 9, 29, 17, 0, 0, DateTimeKind.Utc)) };

        Assert.Equal(new DateOnly(2026, 9, 30), sut.TripDay(loads));
    }

    #endregion

    private static Load PickingUpAt(DateTime at) => new()
    {
        Name = "Drop",
        Type = LoadType.RefrigeratedGoods,
        Customer = null!,
        OriginAddress = null!,
        OriginLocation = null!,
        DestinationAddress = null!,
        DestinationLocation = null!,
        DeliveryCost = null!,
        RequestedPickupDate = at
    };
}
