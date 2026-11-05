using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

[RequiresFeature(TenantFeature.AgenticDispatch)]
public class RenameAIDispatchConversationCommand : ICommand
{
    public Guid ConversationId { get; set; }
    public string Title { get; set; } = "";
}
