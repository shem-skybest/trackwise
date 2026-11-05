using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

internal sealed class CancelAIDispatchTurnHandler(
    IAgentConversationCommands commands) : IAppRequestHandler<CancelAIDispatchTurnCommand, Result>
{
    public Task<Result> Handle(CancelAIDispatchTurnCommand request, CancellationToken ct) =>
        commands.CancelTurnAsync(AgentConversationScope.Dispatch, request.ConversationId, ct);
}
