using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Queries;

internal sealed class GetAICopilotConversationByIdHandler(
    IAgentConversationQueries queries,
    ICurrentUserService currentUser)
    : IAppRequestHandler<GetAICopilotConversationByIdQuery, Result<AgentConversationDto>>
{
    public Task<Result<AgentConversationDto>> Handle(
        GetAICopilotConversationByIdQuery request, CancellationToken ct) =>
        queries.GetByIdAsync(
            AgentConversationScope.Copilot(currentUser.GetUserId()),
            request.Id, includeSessions: false, ct);
}
