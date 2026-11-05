using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.Payments.Stripe;

namespace Trackwise.Application.Modules.IdentityAccess.Subscriptions.Queries;

internal sealed class GetBillingPortalUrlHandler(
    IStripePortalService stripePortalService,
    ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetBillingPortalUrlQuery, Result<BillingPortalUrlDto>>
{
    public async Task<Result<BillingPortalUrlDto>> Handle(
        GetBillingPortalUrlQuery req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();

        if (string.IsNullOrEmpty(tenant.StripeCustomerId))
        {
            return Result<BillingPortalUrlDto>.Fail("Tenant does not have a Stripe customer ID.");
        }

        var portalUrl = await stripePortalService.CreateBillingPortalSessionAsync(
            tenant.StripeCustomerId, req.ReturnUrl, ct);

        return Result<BillingPortalUrlDto>.Ok(new BillingPortalUrlDto { Url = portalUrl });
    }
}
