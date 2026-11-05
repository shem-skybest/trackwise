using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

[RequiresFeature(TenantFeature.AICopilot)]
public class SendAICopilotMessageCommand : ICommand<Result<SendAgentMessageResultDto>>
{
    public Guid ConversationId { get; set; }
    public string Text { get; set; } = "";
}
