using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Queries;

internal sealed class GetAIDispatchConversationByIdHandler(
    IAgentConversationQueries queries)
    : IAppRequestHandler<GetAIDispatchConversationByIdQuery, Result<AgentConversationDto>>
{
    public Task<Result<AgentConversationDto>> Handle(
        GetAIDispatchConversationByIdQuery request, CancellationToken ct) =>
        queries.GetByIdAsync(
            AgentConversationScope.Dispatch, request.Id, includeSessions: true, ct);
}
