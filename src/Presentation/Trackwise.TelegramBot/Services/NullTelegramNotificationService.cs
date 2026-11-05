using Trackwise.Application.Abstractions.Notifications;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.TelegramBot.Services;

/// <summary>
/// No-op stand-in registered when no Telegram bot token is configured, so that notification
/// handlers depending on <see cref="ITelegramNotificationService"/> still resolve.
/// </summary>
internal sealed class NullTelegramNotificationService : ITelegramNotificationService
{
    public Task SendNotificationAsync(
        Guid tenantId,
        string title,
        string message,
        TelegramChatRole? targetRole = null,
        CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
