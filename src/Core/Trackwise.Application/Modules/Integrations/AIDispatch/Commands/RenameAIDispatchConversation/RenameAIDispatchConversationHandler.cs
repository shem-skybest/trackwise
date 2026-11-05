using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

internal sealed class RenameAIDispatchConversationHandler(
    IAgentConversationCommands commands) : IAppRequestHandler<RenameAIDispatchConversationCommand, Result>
{
    public Task<Result> Handle(RenameAIDispatchConversationCommand request, CancellationToken ct) =>
        commands.RenameAsync(
            AgentConversationScope.Dispatch, request.ConversationId, request.Title, ct);
}
