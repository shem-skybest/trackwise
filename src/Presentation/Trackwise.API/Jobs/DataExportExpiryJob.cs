using Trackwise.Application.Modules.Compliance.Privacy.Services;
using Hangfire;

namespace Trackwise.API.Jobs;

public static class DataExportExpiryJob
{
    public static void ScheduleJobs()
    {
        // Daily at 05:00 UTC.
        RecurringJob.AddOrUpdate<IDataExportExpiryService>(
            "privacy-export-expiry",
            x => x.ExpireAsync(default),
            Cron.Daily(5));
    }
}
