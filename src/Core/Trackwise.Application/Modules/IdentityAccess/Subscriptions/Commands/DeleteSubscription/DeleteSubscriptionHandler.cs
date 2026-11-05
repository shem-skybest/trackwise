using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Payments.Stripe;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Commands;

internal sealed class DeleteSubscriptionHandler(
    IMasterUnitOfWork masterUow,
    IStripeSubscriptionService stripeSubscriptionService,
    ILogger<DeleteSubscriptionHandler> logger) : IAppRequestHandler<DeleteSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        DeleteSubscriptionCommand req, CancellationToken ct)
    {
        var subscription = await masterUow.Repository<Subscription>().GetByIdAsync(req.Id, ct);

        if (subscription is null)
        {
            return Result.Fail($"Could not find a subscription with ID '{req.Id}'");
        }

        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            logger.LogInformation("Cancelling stripe subscription {StripeSubscriptionId}",
                subscription.StripeSubscriptionId);
            await stripeSubscriptionService.CancelSubscriptionAsync(subscription.StripeSubscriptionId);
        }

        masterUow.Repository<Subscription>().Delete(subscription);
        await masterUow.SaveChangesAsync(ct);
        logger.LogInformation("Deleted subscription {SubscriptionId}", subscription.Id);
        return Result.Ok();
    }
}
