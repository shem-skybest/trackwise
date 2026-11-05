using Hangfire;
using Trackwise.Application.Modules.Compliance.VehiclePermits.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.API.Jobs;

/// <summary>
/// Daily digest of vehicle papers (LTO registration, LTFRB franchise, insurance...) coming up for
/// renewal. Not feature-gated: a lapsed registration is a legal problem on every plan.
/// </summary>
public class VehiclePermitReminderJob(
    ILogger<VehiclePermitReminderJob> logger,
    IServiceScopeFactory scopeFactory)
{
    /// <summary>22:00 UTC is early morning across Asia-Pacific, so the digest is waiting when offices open.</summary>
    public static void ScheduleJobs()
    {
        RecurringJob.AddOrUpdate<VehiclePermitReminderJob>(
            "vehicle-permit-reminders",
            job => job.RemindAllTenantsAsync(CancellationToken.None),
            Cron.Daily(22));
    }

    [AutomaticRetry(Attempts = 2)]
    public Task RemindAllTenantsAsync(CancellationToken ct) =>
        TenantJobRunner.ForEachTenantAsync(scopeFactory, logger, "vehicle permit reminders", RemindTenantAsync, ct);

    private async Task RemindTenantAsync(IServiceScope scope, Tenant tenant, CancellationToken ct)
    {
        var tenantUow = scope.ServiceProvider.GetRequiredService<ITenantUnitOfWork>();
        tenantUow.SetCurrentTenant(tenant);

        var reminders = scope.ServiceProvider.GetRequiredService<IVehiclePermitReminderService>();
        var count = await reminders.SendDueRemindersAsync(DateOnly.FromDateTime(DateTime.UtcNow), ct);

        if (count > 0)
        {
            logger.LogInformation("Sent a vehicle paper digest covering {Count} papers for tenant {Tenant}", count, tenant.Name);
        }
    }
}
