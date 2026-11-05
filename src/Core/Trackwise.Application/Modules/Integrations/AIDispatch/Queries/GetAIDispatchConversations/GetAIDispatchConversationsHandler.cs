using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Queries;

/// <summary>Tenant-shared: every conversation with Kind == Dispatch, regardless of who created it.</summary>
internal sealed class GetAIDispatchConversationsHandler(
    IAgentConversationQueries queries)
    : IAppRequestHandler<GetAIDispatchConversationsQuery, PagedResult<AgentConversationDto>>
{
    public Task<PagedResult<AgentConversationDto>> Handle(
        GetAIDispatchConversationsQuery request, CancellationToken ct) =>
        queries.ListAsync(AgentConversationScope.Dispatch, request.Page, request.PageSize, ct);
}
