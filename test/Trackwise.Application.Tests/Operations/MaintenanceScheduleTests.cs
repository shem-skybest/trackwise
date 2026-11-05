using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Primitives.Enums.Maintenance;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// A schedule says when a truck's next service is due. Cold Chain's reefer PMS is weekly by date,
/// and most units have no service on record, so "never done" has to mean "due now".
/// </summary>
public class MaintenanceScheduleTests
{
    private static readonly DateTime Sept22 = new(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc);

    private static MaintenanceSchedule Schedule() => new()
    {
        TruckId = Guid.NewGuid(),
        MaintenanceType = MaintenanceType.ReeferPms,
        IntervalType = MaintenanceIntervalType.TimeBased
    };

    #region Interval

    [Fact]
    public void SetInterval_WeeklyAfterALastService_DueAWeekLater()
    {
        var schedule = Schedule();
        schedule.LastServiceDate = Sept22;

        schedule.SetInterval(MaintenanceIntervalType.TimeBased, null, 7, null);

        Assert.Equal(Sept22.AddDays(7), schedule.NextDueDate);
    }

    [Fact]
    public void SetInterval_NoServiceOnRecord_DueToday()
    {
        var schedule = Schedule();

        schedule.SetInterval(MaintenanceIntervalType.TimeBased, null, 7, null);

        Assert.Equal(DateTime.UtcNow.Date, schedule.NextDueDate);
    }

    [Fact]
    public void SetInterval_KeepsOnlyTheIntervalTheTypeUses()
    {
        var schedule = Schedule();

        schedule.SetInterval(MaintenanceIntervalType.TimeBased, 5000, 7, 250);

        Assert.Equal((null, 7, null), (schedule.MileageInterval, schedule.DaysInterval, schedule.EngineHoursInterval));
        Assert.Null(schedule.NextDueMileage);
    }

    [Fact]
    public void SetInterval_DistanceBasedWithAReading_DueAtReadingPlusInterval()
    {
        var schedule = Schedule();
        schedule.LastServiceMileage = 120_000;

        schedule.SetInterval(MaintenanceIntervalType.Mileage, 5000, null, null);

        Assert.Equal(125_000, schedule.NextDueMileage);
        Assert.Null(schedule.NextDueDate);
    }

    [Theory]
    [InlineData(MaintenanceIntervalType.TimeBased, 5000, null, null)]
    [InlineData(MaintenanceIntervalType.Mileage, null, 7, null)]
    [InlineData(MaintenanceIntervalType.EngineHours, null, 0, null)]
    [InlineData(MaintenanceIntervalType.Combined, null, 7, null)]
    public void SetInterval_MissingWhatTheTypeNeeds_Throws(
        MaintenanceIntervalType type, int? distance, int? days, int? hours)
    {
        Assert.Throws<InvalidOperationException>(() => Schedule().SetInterval(type, distance, days, hours));
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void IsOverdue_OnlyOnceItsDueDayHasPassed(int daysFromToday, bool overdue)
    {
        var schedule = Schedule();
        schedule.NextDueDate = DateTime.UtcNow.Date.AddDays(daysFromToday).AddHours(2);

        Assert.Equal(overdue, schedule.IsOverdue);
        Assert.Equal(daysFromToday, schedule.DaysUntilDue(DateTime.UtcNow));
    }

    #endregion

    #region Recording a service

    [Fact]
    public void RecordService_MovesTheScheduleOnFromTheServiceDate()
    {
        var schedule = Schedule();
        schedule.SetInterval(MaintenanceIntervalType.TimeBased, null, 7, null);

        schedule.RecordService(Sept22, 88_400, null);

        Assert.Equal(Sept22, schedule.LastServiceDate);
        Assert.Equal(88_400, schedule.LastServiceMileage);
        Assert.Equal(Sept22.AddDays(7), schedule.NextDueDate);
    }

    [Fact]
    public void RecordService_OlderThanTheLastOne_ChangesNothing()
    {
        var schedule = Schedule();
        schedule.LastServiceDate = Sept22;
        schedule.SetInterval(MaintenanceIntervalType.TimeBased, null, 7, null);

        schedule.RecordService(Sept22.AddDays(-14), null, null);

        Assert.Equal(Sept22, schedule.LastServiceDate);
        Assert.Equal(Sept22.AddDays(7), schedule.NextDueDate);
    }

    [Fact]
    public void RecordService_NoReading_KeepsTheLastReading()
    {
        var schedule = Schedule();
        schedule.LastServiceMileage = 120_000;
        schedule.SetInterval(MaintenanceIntervalType.Combined, 5000, 30, null);

        schedule.RecordService(Sept22, null, null);

        Assert.Equal(125_000, schedule.NextDueMileage);
        Assert.Equal(Sept22.AddDays(30), schedule.NextDueDate);
    }

    #endregion
}
