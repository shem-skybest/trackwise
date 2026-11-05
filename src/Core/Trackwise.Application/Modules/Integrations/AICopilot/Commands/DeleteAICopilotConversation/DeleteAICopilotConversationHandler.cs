using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

internal sealed class DeleteAICopilotConversationHandler(
    IAgentConversationCommands commands,
    ICurrentUserService currentUser) : IAppRequestHandler<DeleteAICopilotConversationCommand, Result>
{
    public Task<Result> Handle(DeleteAICopilotConversationCommand request, CancellationToken ct) =>
        commands.DeleteAsync(
            AgentConversationScope.Copilot(currentUser.GetUserId()), request.ConversationId, ct);
}
