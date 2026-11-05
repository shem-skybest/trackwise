using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Payments.Stripe;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Commands;

internal sealed class CreateSubscriptionPlanHandler(
    IMasterUnitOfWork masterUow,
    IStripePlanService stripePlanService,
    ILogger<CreateSubscriptionPlanHandler> logger) : IAppRequestHandler<CreateSubscriptionPlanCommand, Result>
{
    public async Task<Result> Handle(
        CreateSubscriptionPlanCommand req, CancellationToken ct)
    {
        var subscriptionPlan = new SubscriptionPlan
        {
            Name = req.Name,
            Description = req.Description,
            Price = new() { Amount = req.Price, Currency = "USD" },
            Interval = req.Interval,
            IntervalCount = req.IntervalCount,
            Tier = req.Tier,
            PerTruckPrice = new() { Amount = req.PerTruckPrice, Currency = "USD" },
            MaxTrucks = req.MaxTrucks,
            WeeklyAIBudgetUsd = req.WeeklyAIBudgetUsd
        };

        var result = await stripePlanService.CreatePlanAsync(subscriptionPlan);

        subscriptionPlan.StripeProductId = result.Product.Id;
        subscriptionPlan.StripePriceId = result.BasePrice.Id;
        subscriptionPlan.StripePerTruckPriceId = result.PerTruckPrice.Id;
        subscriptionPlan.StripeAIOveragePriceId = result.AIOveragePrice?.Id;
        await masterUow.Repository<SubscriptionPlan>().AddAsync(subscriptionPlan, ct);
        await masterUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
