using Trackwise.Application.Abstractions.Common;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Services;

public interface IVehiclePermitReminderService : IApplicationService
{
    /// <summary>
    /// Sends the current tenant one digest of vehicle papers crossing a reminder threshold (60, 30
    /// and 7 days out, then on expiry). The caller sets the tenant first. Returns how many papers it covered.
    /// </summary>
    Task<int> SendDueRemindersAsync(DateOnly today, CancellationToken ct = default);
}
