using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

internal sealed class CreatePaymentHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreatePaymentCommand, Result>
{
    public async Task<Result> Handle(
        CreatePaymentCommand req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();
        var currency = (tenant.Settings?.Currency ?? CurrencyCode.USD).ToString();

        var payment = new Payment
        {
            Amount = new() { Amount = req.Amount, Currency = currency },
            StripePaymentMethodId = req.StripePaymentMethodId,
            TenantId = tenant.Id,
            BillingAddress = req.BillingAddress!,
            Description = req.Description
        };

        await tenantUow.Repository<Payment>().AddAsync(payment);
        await tenantUow.SaveChangesAsync();
        return Result.Ok();
    }
}
