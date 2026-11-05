using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.TelegramBot.Authentication;

internal sealed record TelegramChatContext(
    Guid TenantId,
    Guid? UserId,
    TelegramChatType ChatType,
    TelegramChatRole? Role,
    string TenantName);
