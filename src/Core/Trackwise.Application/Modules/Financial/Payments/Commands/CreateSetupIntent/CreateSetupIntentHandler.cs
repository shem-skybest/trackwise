using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Payments.Stripe;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

internal sealed class CreateSetupIntentHandler(
    ITenantUnitOfWork tenantUow,
    IStripePaymentService stripePaymentService,
    ILogger<CreateSetupIntentHandler> logger) : IAppRequestHandler<CreateSetupIntentCommand, Result<SetupIntentDto>>
{
    public async Task<Result<SetupIntentDto>> Handle(CreateSetupIntentCommand req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();

        var setupIntent = await stripePaymentService.CreateSetupIntentAsync(tenant);

        var dto = new SetupIntentDto
        {
            ClientSecret = setupIntent.ClientSecret
        };
        return Result<SetupIntentDto>.Ok(dto);
    }
}
