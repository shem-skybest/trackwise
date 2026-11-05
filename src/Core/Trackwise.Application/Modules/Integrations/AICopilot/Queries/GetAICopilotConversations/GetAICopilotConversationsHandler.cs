using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Queries;

internal sealed class GetAICopilotConversationsHandler(
    IAgentConversationQueries queries,
    ICurrentUserService currentUser)
    : IAppRequestHandler<GetAICopilotConversationsQuery, PagedResult<AgentConversationDto>>
{
    public Task<PagedResult<AgentConversationDto>> Handle(
        GetAICopilotConversationsQuery request, CancellationToken ct) =>
        queries.ListAsync(
            AgentConversationScope.Copilot(currentUser.GetUserId()),
            request.Page, request.PageSize, ct);
}
