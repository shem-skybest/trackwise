using Trackwise.Domain.Primitives.Enums;
using Trackwise.Application.Abstractions.Notifications;

namespace Trackwise.Application.Abstractions.Notifications;

public interface ITelegramNotificationService
{
    /// <summary>
    /// Sends a Telegram notification to all users with the specified role in the tenant.
    /// </summary>
    Task SendNotificationAsync(
        Guid tenantId,
        string title,
        string message,
        TelegramChatRole? targetRole = null,
        CancellationToken ct = default);
}
