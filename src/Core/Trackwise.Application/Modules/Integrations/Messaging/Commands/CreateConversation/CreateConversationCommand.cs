using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Messaging;

namespace Trackwise.Application.Modules.Integrations.Messaging.Commands;

public class CreateConversationCommand : ICommand<Result<ConversationDto>>
{
    public string? Name { get; set; }
    public Guid? LoadId { get; set; }
    public required List<Guid> ParticipantIds { get; set; }
}
