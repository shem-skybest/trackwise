using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;
using Trackwise.Shared.Models.Messaging;

namespace Trackwise.Application.Modules.Integrations.Messaging.Queries;

public class GetConversationsQuery : IQuery<Result<ConversationDto[]>>
{
    /// <summary>
    /// Filter conversations by participant employee ID.
    /// </summary>
    public Guid? ParticipantId { get; set; }

    /// <summary>
    /// Filter conversations by load ID.
    /// </summary>
    public Guid? LoadId { get; set; }
}
