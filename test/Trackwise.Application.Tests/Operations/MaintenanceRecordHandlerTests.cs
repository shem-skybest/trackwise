using Trackwise.Application.Modules.Operations.Maintenance.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Maintenance;
using Trackwise.Shared.Models;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// Logging a repair or a PMS: the parts add up to the parts cost, and a service finds the truck's
/// schedule of the same type by itself, so the weekly PMS moves on without anyone linking it.
/// </summary>
public class MaintenanceRecordHandlerTests
{
    private static readonly DateTime Sept22 = new(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<MaintenanceRecord, Guid> recordRepo = Substitute.For<ITenantRepository<MaintenanceRecord, Guid>>();
    private readonly ITenantRepository<MaintenancePart, Guid> partRepo = Substitute.For<ITenantRepository<MaintenancePart, Guid>>();
    private readonly ITenantRepository<MaintenanceSchedule, Guid> scheduleRepo = Substitute.For<ITenantRepository<MaintenanceSchedule, Guid>>();
    private readonly List<MaintenanceSchedule> schedules = [];
    private readonly CreateMaintenanceRecordHandler sut;

    private readonly Truck truck = new() { Number = "ABC1267", Name = "ABC1267", Type = TruckType.Reefer };

    public MaintenanceRecordHandlerTests()
    {
        var truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
        truckRepo.GetByIdAsync(truck.Id, Arg.Any<CancellationToken>()).Returns(truck);
        truckRepo.Query().Returns(new[] { truck }.BuildMock());
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<MaintenanceRecord>().Returns(recordRepo);
        tenantUow.Repository<MaintenancePart>().Returns(partRepo);
        tenantUow.Repository<MaintenanceSchedule>().Returns(scheduleRepo);
        scheduleRepo.Query().Returns(_ => schedules.BuildMock());
        sut = new CreateMaintenanceRecordHandler(tenantUow);
    }

    private MaintenanceSchedule AddWeeklyPms(Guid? truckId = null)
    {
        var schedule = new MaintenanceSchedule
        {
            TruckId = truckId ?? truck.Id,
            MaintenanceType = MaintenanceType.ReeferPms,
            IntervalType = MaintenanceIntervalType.TimeBased
        };
        schedule.SetInterval(MaintenanceIntervalType.TimeBased, null, 7, null);
        schedules.Add(schedule);
        return schedule;
    }

    private CreateMaintenanceRecordCommand Command(MaintenanceType type = MaintenanceType.Repair) => new()
    {
        TruckId = truck.Id,
        Type = type,
        Description = "Leaf spring and center bolt",
        ServiceDate = Sept22,
        RequisitionNumber = " rfv26-001517-m ",
        LaborCost = 2500,
        PartsCost = 999,
        Parts =
        [
            new MaintenancePartRequest { PartName = "LEAF SPRING", Quantity = 2, UnitCost = 1800 },
            new MaintenancePartRequest { PartName = "CENTER BOLT", Quantity = 4, UnitCost = 60 }
        ]
    };

    [Fact]
    public async Task Handle_PartLines_PartsCostIsTheirSumNotTheTypedAmount()
    {
        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((3840m, 2500m, 6340m), (result.Value!.PartsCost, result.Value.LaborCost, result.Value.TotalCost));
        await partRepo.Received(2).AddAsync(Arg.Any<MaintenancePart>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPartLines_KeepsTheTypedPartsCost()
    {
        var command = Command() with { Parts = [] };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(999m, result.Value!.PartsCost);
    }

    [Fact]
    public async Task Handle_RequisitionNumber_IsTrimmedAndUpperCased()
    {
        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.Equal("RFV26-001517-M", result.Value!.RequisitionNumber);
    }

    [Fact]
    public async Task Handle_TruckHasAScheduleOfThatType_LinksItAndMovesItOn()
    {
        var pms = AddWeeklyPms();

        var result = await sut.Handle(Command(MaintenanceType.ReeferPms), CancellationToken.None);

        Assert.Equal(pms.Id, result.Value!.MaintenanceScheduleId);
        Assert.Equal(Sept22.AddDays(7), pms.NextDueDate);
    }

    [Fact]
    public async Task Handle_NoScheduleOfThatType_LeavesOthersAlone()
    {
        var pms = AddWeeklyPms();
        var due = pms.NextDueDate;

        var result = await sut.Handle(Command(), CancellationToken.None);

        Assert.Null(result.Value!.MaintenanceScheduleId);
        Assert.Equal(due, pms.NextDueDate);
    }

    [Fact]
    public async Task Handle_ScheduleOfAnotherTruck_FailsWithoutSaving()
    {
        var otherTrucksPms = AddWeeklyPms(Guid.NewGuid());
        var command = Command(MaintenanceType.ReeferPms) with { MaintenanceScheduleId = otherTrucksPms.Id };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
