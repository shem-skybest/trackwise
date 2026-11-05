using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.AIDispatch;
using Trackwise.Application.Abstractions.BackgroundJobs;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

/// <summary>
/// Dispatch conversations are tenant-shared: no ownership check, only the endpoint's
/// <c>Permission.Dispatch.Manage</c> policy gates who may send. <c>CreatedById</c> stays whoever
/// created the conversation, for audit only.
/// </summary>
internal sealed class SendAIDispatchMessageHandler(
    IAgentConversationCommands commands,
    ICurrentUserService currentUser,
    IBackgroundJobRunner<AIDispatchTurnRequest> backgroundRunner,
    IAIDispatchBroadcastService broadcastService)
    : IAppRequestHandler<SendAIDispatchMessageCommand, Result<SendAgentMessageResultDto>>
{
    public Task<Result<SendAgentMessageResultDto>> Handle(
        SendAIDispatchMessageCommand request, CancellationToken ct) =>
        commands.SendMessageAsync(
            AgentConversationScope.Dispatch,
            request.ConversationId,
            request.Text,
            currentUser.GetUserId(),
            (tenantId, conversationId, userId) =>
                backgroundRunner.Enqueue(new AIDispatchTurnRequest(tenantId, conversationId, userId)),
            ct,
            // The board is shared: without this the others see the agent's answer but not the question.
            (tenantId, message) => broadcastService.BroadcastMessageAsync(tenantId, message));
}
