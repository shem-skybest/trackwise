using System.Text;
using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions.Notifications;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Services;

/// <summary>
/// One digest per run rather than one notification per paper: a fleet's registrations cluster by
/// plate ending, and a first run over imported papers can find dozens due at once.
/// </summary>
internal sealed class VehiclePermitReminderService(
    ITenantUnitOfWork tenantUow,
    INotificationService notificationService)
    : IVehiclePermitReminderService
{
    private const int MaxListed = 15;

    public async Task<int> SendDueRemindersAsync(DateOnly today, CancellationToken ct = default)
    {
        var horizon = today.AddDays(VehiclePermitRules.ReminderThresholds[0]);

        // Every paper of a vehicle/type that has one near expiry, so a renewal already on file
        // (a later record of the same type) can mark the old one as superseded.
        var nearExpiry = tenantUow.Repository<VehiclePermit>().Query().Where(p => p.ExpiresOn <= horizon);
        var permits = await tenantUow.Repository<VehiclePermit>().Query()
            .Where(p => nearExpiry.Any(n => n.TruckId == p.TruckId && n.TrailerId == p.TrailerId && n.Type == p.Type))
            .ToListAsync(ct);

        var current = VehiclePermitRules.CurrentIds(permits);
        var vehicles = await VehicleLookup.LoadAsync(tenantUow, permits, ct);

        var due = permits
            .Where(p => current.Contains(p.Id) && vehicles.For(p) is { IsActive: true })
            .Select(p => (Permit: p, Threshold: VehiclePermitRules.DueThreshold(p.DaysUntilExpiry(today), p.LastReminderThresholdDays)))
            .Where(x => x.Threshold.HasValue)
            .OrderBy(x => x.Permit.ExpiresOn)
            .ToList();

        if (due.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var (permit, threshold) in due)
        {
            permit.MarkReminded(threshold!.Value, now);
        }

        // The notification service saves the unit of work, which also persists the reminder marks.
        await notificationService.SendNotificationAsync(
            Title(due.Count(x => x.Permit.DaysUntilExpiry(today) < 0), due.Count),
            Message(due.Select(x => x.Permit), vehicles, today));

        return due.Count;
    }

    private static string Title(int expired, int total)
    {
        var expiring = total - expired;
        return (expired, expiring) switch
        {
            (0, _) => $"Vehicle papers: {expiring} expiring soon",
            (_, 0) => $"Vehicle papers: {expired} expired",
            _ => $"Vehicle papers: {expired} expired, {expiring} expiring soon"
        };
    }

    private static string Message(IEnumerable<VehiclePermit> permits, Dictionary<Guid, VehicleRef> vehicles, DateOnly today)
    {
        var list = permits.ToList();
        var text = new StringBuilder();
        foreach (var permit in list.Take(MaxListed))
        {
            var days = permit.DaysUntilExpiry(today);
            var when = days switch
            {
                < 0 => $"expired {permit.ExpiresOn:yyyy-MM-dd}",
                0 => "expires today",
                _ => $"expires {permit.ExpiresOn:yyyy-MM-dd} ({days} days)"
            };
            text.AppendLine($"{vehicles.For(permit)?.Code}: {permit.Type.GetDescription()} {when}");
        }

        if (list.Count > MaxListed)
        {
            text.AppendLine($"...and {list.Count - MaxListed} more.");
        }

        return text.ToString().TrimEnd();
    }
}
