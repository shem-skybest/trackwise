using Trackwise.Application.Modules.Financial.Tax.Services;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

internal sealed class CreateLoadInvoiceHandler(
    ITenantUnitOfWork tenantUow,
    IInvoiceTaxApplier taxApplier)
    : IAppRequestHandler<CreateLoadInvoiceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateLoadInvoiceCommand req, CancellationToken ct)
    {
        var load = await tenantUow.Repository<Load>().GetByIdAsync(req.LoadId, ct);

        if (load is null)
        {
            return Result<Guid>.Fail($"Could not find a load with ID '{req.LoadId}'");
        }

        var customer = await tenantUow.Repository<Customer>().GetByIdAsync(req.CustomerId, ct);

        if (customer is null)
        {
            return Result<Guid>.Fail($"Could not find a customer with ID '{req.CustomerId}'");
        }

        var tenant = tenantUow.GetCurrentTenant();
        var currency = (tenant.Settings?.Currency ?? CurrencyCode.USD).ToString();

        var amount = new Money { Amount = req.PaymentAmount, Currency = currency };
        var invoice = new LoadInvoice
        {
            Subtotal = amount,
            TaxTotal = Money.Zero(currency),
            Total = amount,
            CustomerId = req.CustomerId,
            Customer = customer,
            LoadId = req.LoadId
        };

        if (req.RecordPayment)
        {
            var payment = new Payment
            {
                StripePaymentMethodId = req.StripePaymentMethodId,
                TenantId = tenant.Id,
                Amount = new() { Amount = req.PaymentAmount, Currency = currency },
                BillingAddress = tenant.CompanyAddress
            };

            invoice.ApplyPayment(payment);
        }

        await taxApplier.ApplyAsync(invoice, ct);

        await tenantUow.Repository<Invoice>().AddAsync(invoice, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<Guid>.Ok(invoice.Id);
    }
}
