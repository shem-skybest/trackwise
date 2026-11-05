using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.AIDispatch;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Integrations.AIDispatch.Services;
using Trackwise.Application.Modules.Integrations.Agents.Services;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.AIDispatch.Commands;

internal sealed class RejectAIDispatchDecisionHandler(
    ITenantUnitOfWork tenantUow,
    IAIDispatchDecisionGuard guard,
    IAgentDecisionNotes notes,
    ICurrentUserService currentUser,
    IAIDispatchBroadcastService broadcastService) : IAppRequestHandler<RejectAIDispatchDecisionCommand, Result>
{
    public async Task<Result> Handle(RejectAIDispatchDecisionCommand request, CancellationToken ct)
    {
        var loaded = await guard.LoadAsync(request.DecisionId, ct);
        if (!loaded.IsSuccess)
            return Result.Fail(loaded.Error!);

        var decision = loaded.Value!;
        var userId = currentUser.GetUserId() ?? Guid.Empty;
        decision.Reject(userId, request.Reason);

        var tenantId = tenantUow.GetCurrentTenant().Id;
        var conversation = await notes.LoadConversationAsync(decision, ct);

        await notes.AppendAsync(
            conversation, notes.RejectionNote(decision, request.Reason), userId,
            message => broadcastService.BroadcastMessageAsync(tenantId, message), ct);

        return Result.Ok();
    }
}
