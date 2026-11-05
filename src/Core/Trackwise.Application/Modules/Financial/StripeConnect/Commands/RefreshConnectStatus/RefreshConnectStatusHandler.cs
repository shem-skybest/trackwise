using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Payments.Stripe;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Financial.StripeConnect.Commands;

internal sealed class RefreshConnectStatusHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IStripeConnectService stripeConnectService,
    ILogger<RefreshConnectStatusHandler> logger)
    : IAppRequestHandler<RefreshConnectStatusCommand, Result<StripeConnectStatusDto>>
{
    public async Task<Result<StripeConnectStatusDto>> Handle(RefreshConnectStatusCommand req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();

        // If no connected account, return not connected status
        if (string.IsNullOrEmpty(tenant.StripeConnectedAccountId))
        {
            return Result<StripeConnectStatusDto>.Ok(new StripeConnectStatusDto
            {
                AccountId = null,
                Status = StripeConnectStatus.NotConnected,
                ChargesEnabled = false,
                PayoutsEnabled = false
            });
        }

        try
        {
            // Sync the latest status from Stripe
            await stripeConnectService.SyncConnectedAccountStatusAsync(tenant);

            // Save the updated status
            masterUow.Repository<Domain.Entities.Tenant>().Update(tenant);
            await masterUow.SaveChangesAsync(ct);

            return Result<StripeConnectStatusDto>.Ok(new StripeConnectStatusDto
            {
                AccountId = tenant.StripeConnectedAccountId,
                Status = tenant.ConnectStatus,
                ChargesEnabled = tenant.ChargesEnabled,
                PayoutsEnabled = tenant.PayoutsEnabled
            });
        }
        catch (Stripe.StripeException ex)
        {
            logger.LogError(ex, "Failed to refresh Connect status for tenant {TenantId}", tenant.Id);

            // Return cached status if Stripe call fails
            return Result<StripeConnectStatusDto>.Ok(new StripeConnectStatusDto
            {
                AccountId = tenant.StripeConnectedAccountId,
                Status = tenant.ConnectStatus,
                ChargesEnabled = tenant.ChargesEnabled,
                PayoutsEnabled = tenant.PayoutsEnabled
            });
        }
    }
}
