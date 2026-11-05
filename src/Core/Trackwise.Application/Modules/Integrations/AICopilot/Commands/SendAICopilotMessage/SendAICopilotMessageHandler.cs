using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.AICopilot;
using Trackwise.Application.Abstractions.BackgroundJobs;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

internal sealed class SendAICopilotMessageHandler(
    IAgentConversationCommands commands,
    ICurrentUserService currentUser,
    IBackgroundJobRunner<AICopilotTurnRequest> backgroundRunner)
    : IAppRequestHandler<SendAICopilotMessageCommand, Result<SendAgentMessageResultDto>>
{
    public Task<Result<SendAgentMessageResultDto>> Handle(
        SendAICopilotMessageCommand request, CancellationToken ct)
    {
        var userId = currentUser.GetUserId();

        return commands.SendMessageAsync(
            AgentConversationScope.Copilot(userId),
            request.ConversationId,
            request.Text,
            userId,
            (tenantId, conversationId, callerId) =>
                backgroundRunner.Enqueue(new AICopilotTurnRequest(tenantId, conversationId, callerId)),
            ct);
    }
}
