using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.AICopilot;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.AICopilot.Services;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AICopilot.Commands;

internal sealed class RejectAICopilotDecisionHandler(
    ITenantUnitOfWork tenantUow,
    IAICopilotDecisionGuard guard,
    IAgentDecisionNotes notes,
    ICurrentUserService currentUser,
    IAICopilotBroadcastService broadcastService) : IAppRequestHandler<RejectAICopilotDecisionCommand, Result>
{
    public async Task<Result> Handle(RejectAICopilotDecisionCommand request, CancellationToken ct)
    {
        var userId = currentUser.GetUserId();
        var loaded = await guard.LoadAsync(request.DecisionId, userId, ct);
        if (!loaded.IsSuccess)
            return Result.Fail(loaded.Error!);

        var (decision, conversation) = loaded.Value!;
        decision.Reject(userId!.Value, request.Reason);

        var tenantId = tenantUow.GetCurrentTenant().Id;

        await notes.AppendAsync(
            conversation, notes.RejectionNote(decision, request.Reason), userId,
            message => broadcastService.BroadcastMessageAsync(tenantId, conversation.CreatedById, message), ct);

        await broadcastService.BroadcastDecisionAsync(tenantId, conversation.CreatedById, decision.ToDto());
        return Result.Ok();
    }
}
