using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Payments.Stripe;

namespace Trackwise.Application.Modules.Financial.StripeConnect.Queries;

internal sealed class GetDashboardLinkHandler(
    ITenantUnitOfWork tenantUow,
    IStripeConnectService stripeConnectService,
    ILogger<GetDashboardLinkHandler> logger)
    : IAppRequestHandler<GetDashboardLinkQuery, Result<DashboardLinkDto>>
{
    public async Task<Result<DashboardLinkDto>> Handle(GetDashboardLinkQuery req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();

        if (string.IsNullOrEmpty(tenant.StripeConnectedAccountId))
        {
            return Result<DashboardLinkDto>.Fail("Tenant does not have a Stripe Connect account.");
        }

        try
        {
            var loginLink = await stripeConnectService.CreateLoginLinkAsync(tenant.StripeConnectedAccountId);

            logger.LogInformation(
                "Created dashboard link for tenant {TenantId}",
                tenant.Id);

            return Result<DashboardLinkDto>.Ok(new DashboardLinkDto
            {
                Url = loginLink.Url
            });
        }
        catch (Stripe.StripeException ex)
        {
            logger.LogError(ex, "Failed to create dashboard link for tenant {TenantId}", tenant.Id);
            return Result<DashboardLinkDto>.Fail($"Failed to create dashboard link: {ex.Message}");
        }
    }
}
