using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal static class MaintenanceScheduleWriter
{
    /// <summary>
    /// A truck keeps one active schedule per service type: a logged service finds its schedule by
    /// type, and two would leave it guessing which one to move on.
    /// </summary>
    public static async Task<Result> EnsureOnlyActiveAsync(
        ITenantUnitOfWork tenantUow, MaintenanceSchedule schedule, CancellationToken ct)
    {
        var clash = await tenantUow.Repository<MaintenanceSchedule>().Query()
            .AnyAsync(s => s.Id != schedule.Id
                           && s.TruckId == schedule.TruckId
                           && s.MaintenanceType == schedule.MaintenanceType
                           && s.IsActive, ct);
        return clash
            ? Result.Fail("This truck already has an active schedule for that service. Edit that one instead.")
            : Result.Ok();
    }

    public static Result Apply(MaintenanceSchedule schedule, IMaintenanceScheduleFields req)
    {
        schedule.LastServiceDate = req.LastServiceDate;
        schedule.LastServiceMileage = req.LastServiceOdometer;
        schedule.LastServiceEngineHours = req.LastServiceEngineHours;
        schedule.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();

        try
        {
            schedule.SetInterval(req.IntervalType, req.MileageInterval, req.DaysInterval, req.EngineHoursInterval);
            return Result.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}
