using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

internal sealed class DeleteAIDispatchConversationHandler(
    IAgentConversationCommands commands) : IAppRequestHandler<DeleteAIDispatchConversationCommand, Result>
{
    public Task<Result> Handle(DeleteAIDispatchConversationCommand request, CancellationToken ct) =>
        commands.DeleteAsync(AgentConversationScope.Dispatch, request.ConversationId, ct);
}
