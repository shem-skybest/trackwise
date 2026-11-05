using Trackwise.Infrastructure.Communications.SignalR.Clients;
using Trackwise.Infrastructure.Communications.SignalR.Hubs;
using Trackwise.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Trackwise.Application.Abstractions.AIDispatch;

namespace Trackwise.Infrastructure.Communications.SignalR.Services;

internal sealed class SignalRAIDispatchBroadcastService(
    IHubContext<AIDispatchHub, IAIDispatchHubClient> hubContext) : IAIDispatchBroadcastService
{
    private const string DispatchBoardGroupPrefix = "dispatch-board:";

    public Task BroadcastMessageAsync(Guid tenantId, AgentMessageDto message) =>
        Group(tenantId).ReceiveDispatchMessage(message);

    public Task BroadcastTurnUpdateAsync(Guid tenantId, AgentTurnUpdateDto update) =>
        Group(tenantId).ReceiveDispatchTurnUpdate(update);

    public Task BroadcastDecisionAsync(Guid tenantId, AgentDecisionDto decision) =>
        Group(tenantId).ReceiveAIDispatchDecision(decision);

    public Task BroadcastNegotiationAsync(Guid tenantId, RateNegotiationDto negotiation) =>
        Group(tenantId).ReceiveNegotiationUpdate(negotiation);

    private IAIDispatchHubClient Group(Guid tenantId) =>
        hubContext.Clients.Group($"{DispatchBoardGroupPrefix}{tenantId}");
}
