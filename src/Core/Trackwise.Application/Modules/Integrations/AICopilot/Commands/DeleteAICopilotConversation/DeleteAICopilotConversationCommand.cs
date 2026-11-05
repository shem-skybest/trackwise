using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

[RequiresFeature(TenantFeature.AICopilot)]
public class DeleteAICopilotConversationCommand : ICommand
{
    public Guid ConversationId { get; set; }
}
