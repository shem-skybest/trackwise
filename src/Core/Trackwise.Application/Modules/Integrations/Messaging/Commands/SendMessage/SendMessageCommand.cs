using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Messaging;

namespace Trackwise.Application.Modules.Integrations.Messaging.Commands;

public class SendMessageCommand : ICommand<Result<MessageDto>>
{
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public required string Content { get; set; }
}
