using Trackwise.Application.Modules.Integrations.Agents;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Services;

internal sealed class AICopilotDecisionGuard(
    ITenantUnitOfWork tenantUow,
    IAgentConversationAccess access) : IAICopilotDecisionGuard
{
    public async Task<Result<CopilotDecisionContext>> LoadAsync(
        Guid decisionId, Guid? userId, CancellationToken ct)
    {
        if (userId is null)
            return Result<CopilotDecisionContext>.Fail("User is not authenticated");

        var decision = await tenantUow.Repository<AgentDecision>().GetByIdAsync(decisionId, ct);
        if (decision is null || decision.Session.Type != AgentSessionType.Copilot)
            return Result<CopilotDecisionContext>.Fail("Decision not found");

        var conversation = await access.LoadAsync(
            decision.Session.ConversationId, AgentConversationScope.Copilot(userId), ct);
        if (conversation is null)
            return Result<CopilotDecisionContext>.Fail("Decision not found");

        if (decision.Status != AgentDecisionStatus.Suggested)
            return Result<CopilotDecisionContext>.Fail("Decision is not in a suggested state");

        return Result<CopilotDecisionContext>.Ok(new CopilotDecisionContext(decision, conversation));
    }
}
