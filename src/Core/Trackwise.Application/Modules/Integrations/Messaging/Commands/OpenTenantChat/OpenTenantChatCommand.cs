using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Messaging;

namespace Trackwise.Application.Modules.Integrations.Messaging.Commands;

/// <summary>
/// Opens (find-or-create) the tenant-wide group chat and ensures the requesting employee is
/// a participant. Returns the conversation DTO so the client can render it immediately.
/// </summary>
public class OpenTenantChatCommand : ICommand<Result<ConversationDto>>
{
    /// <summary>
    /// The ID of the employee opening the chat (for unread count calculation and ensure-participant).
    /// </summary>
    public Guid EmployeeId { get; set; }
}
