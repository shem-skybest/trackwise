using Trackwise.Application.Modules.Financial.StripeConnect.Services;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Stripe;
using Trackwise.Application.Abstractions.Payments.Stripe;
using Subscription = Trackwise.Domain.Entities.Subscription;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Commands;

internal sealed class RenewSubscriptionHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IStripeSubscriptionService stripeSubscriptionService,
    ILogger<RenewSubscriptionHandler> logger) : IAppRequestHandler<RenewSubscriptionCommand, Result>
{
    public async Task<Result> Handle(
        RenewSubscriptionCommand req, CancellationToken ct)
    {
        var subscription = await masterUow.Repository<Subscription>().GetByIdAsync(req.Id, ct);

        if (subscription is null)
        {
            return Result.Fail($"Could not find a subscription with ID '{req.Id}'");
        }

        await tenantUow.SetCurrentTenantByIdAsync(subscription.TenantId);
        var truckCount = await tenantUow.Repository<Truck>().CountAsync(ct: ct);

        try
        {
            logger.LogInformation("Renewing stripe subscription {StripeSubscriptionId}",
                subscription.StripeSubscriptionId);
            var stripeSubscription =
                await stripeSubscriptionService.RenewSubscriptionAsync(subscription, subscription.Plan,
                    subscription.Tenant, truckCount);
            subscription.StripeSubscriptionId = stripeSubscription.Id;
            subscription.Status = StripeObjectMapper.GetSubscriptionStatus(stripeSubscription.Status);
            subscription.CancelAtPeriodEnd = stripeSubscription.CancelAtPeriodEnd;

            masterUow.Repository<Subscription>().Update(subscription);
            await masterUow.SaveChangesAsync(ct);
            logger.LogInformation("Renewed subscription {SubscriptionId}", subscription.Id);
            return Result.Ok();
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe error renewing subscription {SubscriptionId}", subscription.Id);

            var message = ex.StripeError?.Code switch
            {
                "resource_missing" => "No payment method on file. Please add a payment method in Manage Billing before resuming.",
                _ when ex.Message.Contains("payment source or default payment method") =>
                    "No payment method on file. Please add a payment method in Manage Billing before resuming.",
                _ => $"Failed to resume subscription: {ex.Message}"
            };

            return Result.Fail(message);
        }
    }
}
