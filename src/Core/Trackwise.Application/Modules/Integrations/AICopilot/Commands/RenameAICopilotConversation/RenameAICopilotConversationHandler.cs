using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

internal sealed class RenameAICopilotConversationHandler(
    IAgentConversationCommands commands,
    ICurrentUserService currentUser) : IAppRequestHandler<RenameAICopilotConversationCommand, Result>
{
    public Task<Result> Handle(RenameAICopilotConversationCommand request, CancellationToken ct) =>
        commands.RenameAsync(
            AgentConversationScope.Copilot(currentUser.GetUserId()),
            request.ConversationId, request.Title, ct);
}
