using Trackwise.Application.Modules.Financial.StripeConnect.Services;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Payments.Stripe;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Commands;

internal sealed class CancelSubscriptionHandler(
    IMasterUnitOfWork masterUow,
    IStripeSubscriptionService stripeSubscriptionService,
    ILogger<DeleteSubscriptionHandler> logger) : IAppRequestHandler<CancelSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        CancelSubscriptionCommand req, CancellationToken ct)
    {
        var subscription = await masterUow.Repository<Subscription>().GetByIdAsync(req.Id, ct);
        SubscriptionStatus? status = null;

        if (subscription is null)
        {
            return Result.Fail($"Could not find a subscription with ID '{req.Id}'");
        }

        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            logger.LogInformation("Cancelling stripe subscription {StripeSubscriptionId}",
                subscription.StripeSubscriptionId);
            var stripeSubscription =
                await stripeSubscriptionService.CancelSubscriptionAsync(subscription.StripeSubscriptionId, false);
            status = StripeObjectMapper.GetSubscriptionStatus(stripeSubscription.Status);
            subscription.CancelAtPeriodEnd = stripeSubscription.CancelAtPeriodEnd;
        }

        subscription.Status = status ?? SubscriptionStatus.Cancelled;

        masterUow.Repository<Subscription>().Update(subscription);
        await masterUow.SaveChangesAsync(ct);
        logger.LogInformation("Cancelled subscription {SubscriptionId}", subscription.Id);
        return Result.Ok();
    }
}
