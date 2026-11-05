using Trackwise.Application.Abstractions.Agents;
using Trackwise.Application.Modules.IdentityAccess.Users.Services;
using Trackwise.Domain.Entities;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Agents.Services;

internal sealed class AgentDecisionAuthorization(
    IAgentToolRegistry toolRegistry,
    IUserPermissionService userPermissions) : IAgentDecisionAuthorization
{
    public async Task<Result> EnsureToolPermissionAsync(
        AgentDecision decision, Guid userId, Guid tenantId, CancellationToken ct)
    {
        if (toolRegistry.TryGetDefinition(decision.ToolName!)?.RequiredPermission is not { } required)
            return Result.Ok();

        return await userPermissions.HasPermissionAsync(userId, tenantId, required, ct)
            ? Result.Ok()
            : Result.Fail($"You need the {required} permission to approve this action");
    }
}
