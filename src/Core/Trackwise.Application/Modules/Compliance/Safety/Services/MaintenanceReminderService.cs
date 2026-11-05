using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Realtime;

namespace Trackwise.Application.Modules.Compliance.Safety.Services;

internal class MaintenanceReminderService(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IRealtimeNotificationService notificationService,
    ILogger<MaintenanceReminderService> logger)
    : IMaintenanceReminderService
{
    private static readonly int[] ReminderDays = [30, 14, 7, 3, 1, 0];

    public async Task ProcessMaintenanceRemindersAsync(CancellationToken ct = default)
    {
        var tenants = await masterUow.Repository<Tenant>().GetListAsync();

        foreach (var tenant in tenants)
        {
            try
            {
                tenantUow.SetCurrentTenant(tenant);
                await ProcessTenantMaintenanceAsync(tenant, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing maintenance for tenant {TenantName}", tenant.Name);
            }
        }
    }

    private async Task ProcessTenantMaintenanceAsync(Tenant tenant, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var maxDate = today.AddDays(ReminderDays[0]); // 30 days out

        // Get active schedules with upcoming due dates
        var schedules = await tenantUow.Repository<MaintenanceSchedule>()
            .GetListAsync(s =>
                s.IsActive &&
                s.NextDueDate.HasValue &&
                s.NextDueDate <= maxDate);

        var overdueCount = 0;
        var upcomingCount = 0;

        var truckNumbers = await TruckRefResolver.ResolveAsync(
            tenantUow, schedules.Select(s => s.TruckId).Distinct().ToArray(), ct);

        foreach (var schedule in schedules)
        {
            var daysUntilDue = schedule.DaysUntilDue(DateTime.UtcNow) ?? 0;

            var truckNumber = truckNumbers.TryGetValue(schedule.TruckId, out var truckRef)
                ? truckRef.Number
                : "Unknown";

            // Weekly, not daily: a fleet on a weekly PMS would otherwise get a notice per truck every day.
            if (schedule.IsOverdue && (-daysUntilDue - 1) % 7 == 0)
            {
                overdueCount++;
                await SendMaintenanceNotificationAsync(tenant.Id.ToString(), schedule, truckNumber, isOverdue: true);
            }
            else if (!schedule.IsOverdue && ShouldSendReminder(daysUntilDue))
            {
                upcomingCount++;
                await SendMaintenanceNotificationAsync(tenant.Id.ToString(), schedule, truckNumber, isOverdue: false, daysUntilDue);
            }
        }

        logger.LogInformation(
            "Processed {OverdueCount} overdue and {UpcomingCount} upcoming maintenance reminders for tenant {TenantName}",
            overdueCount, upcomingCount, tenant.Name);
    }

    private static bool ShouldSendReminder(int daysUntilDue)
    {
        return ReminderDays.Contains(daysUntilDue);
    }

    private async Task SendMaintenanceNotificationAsync(
        string tenantId,
        MaintenanceSchedule schedule,
        string truckNumber,
        bool isOverdue,
        int daysUntilDue = 0)
    {
        var maintenanceTypeName = schedule.MaintenanceType.GetDescription();

        var notification = new NotificationDto
        {
            Title = isOverdue
                ? $"Overdue Maintenance: {maintenanceTypeName}"
                : $"Upcoming Maintenance: {maintenanceTypeName}",
            Message = isOverdue
                ? $"Truck {truckNumber} has overdue {maintenanceTypeName}. Due date was {schedule.NextDueDate:d}."
                : $"Truck {truckNumber} requires {maintenanceTypeName} in {daysUntilDue} days.",
            CreatedDate = DateTime.UtcNow
        };

        await notificationService.BroadcastNotificationAsync(tenantId, notification);
    }
}
