using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

internal sealed class CancelAICopilotTurnHandler(
    IAgentConversationCommands commands,
    ICurrentUserService currentUser) : IAppRequestHandler<CancelAICopilotTurnCommand, Result>
{
    public Task<Result> Handle(CancelAICopilotTurnCommand request, CancellationToken ct) =>
        commands.CancelTurnAsync(
            AgentConversationScope.Copilot(currentUser.GetUserId()), request.ConversationId, ct);
}
