using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.AIDispatch;
using Trackwise.Application.Modules.Integrations.Negotiation.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Commands;

internal sealed class CloseNegotiationHandler(
    ITenantUnitOfWork tenantUow,
    IInboundEmailRouteRegistry routeRegistry,
    IAIDispatchBroadcastService broadcastService)
    : IAppRequestHandler<CloseNegotiationCommand, Result>
{
    public async Task<Result> Handle(CloseNegotiationCommand req, CancellationToken ct)
    {
        var negotiation = await tenantUow.Repository<RateNegotiation>().GetByIdAsync(req.Id, ct);
        if (negotiation is null)
        {
            return Result.Fail($"Could not find a negotiation with ID '{req.Id}'");
        }

        if (!negotiation.IsOpen)
        {
            return Result.Fail($"This negotiation is already {negotiation.Status.GetDescription().ToLowerInvariant()}.");
        }

        negotiation.Close(
            req.Declined ? RateNegotiationStatus.Declined : RateNegotiationStatus.Closed,
            req.Reason);

        await tenantUow.SaveChangesAsync(ct);

        await routeRegistry.RevokeAsync([negotiation.ReplyToken], ct);

        await NegotiationBroadcast.PublishAsync(tenantUow, broadcastService, negotiation, ct);

        return Result.Ok();
    }
}
