using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Payments.Stripe;
using Trackwise.Application.Abstractions.Payments;
using Trackwise.Application.Abstractions.Payments.Xendit;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

internal sealed class CreatePublicCheckoutSessionHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IStripeConnectService stripeConnectService,
    IXenditClient xenditClient,
    ILogger<CreatePublicCheckoutSessionHandler> logger)
    : IAppRequestHandler<CreatePublicCheckoutSessionCommand, Result<PublicCheckoutSessionDto>>
{
    public async Task<Result<PublicCheckoutSessionDto>> Handle(
        CreatePublicCheckoutSessionCommand req, CancellationToken ct)
    {
        var tenant = await masterUow.Repository<Tenant>().GetByIdAsync(req.TenantId, ct);
        if (tenant is null)
        {
            return Result<PublicCheckoutSessionDto>.Fail("Invalid payment link.");
        }

        tenantUow.SetCurrentTenant(tenant);

        var paymentLink = await tenantUow.Repository<PaymentLink>()
            .GetAsync(p => p.Token == req.Token, ct);

        if (paymentLink is null || !paymentLink.IsValid)
        {
            return Result<PublicCheckoutSessionDto>.Fail(
                "This payment link has expired or been revoked.");
        }

        var invoice = await tenantUow.Repository<Invoice>().GetByIdAsync(paymentLink.InvoiceId, ct);
        if (invoice is null)
        {
            return Result<PublicCheckoutSessionDto>.Fail("Invoice not found.");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Result<PublicCheckoutSessionDto>.Fail("This invoice has already been paid.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return Result<PublicCheckoutSessionDto>.Fail("This invoice has been cancelled.");
        }

        var totalPaid = invoice.Payments.Sum(p => p.Amount.Amount);
        var amountDue = invoice.Total.Amount - totalPaid;
        var paymentAmount = req.Amount ?? amountDue;

        if (paymentAmount <= 0)
        {
            return Result<PublicCheckoutSessionDto>.Fail("Payment amount must be greater than zero.");
        }

        if (paymentAmount > amountDue)
        {
            return Result<PublicCheckoutSessionDto>.Fail(
                $"Payment amount cannot exceed amount due ({amountDue:C}).");
        }

        // A company with its own Xendit account (PH) takes payments there; everyone else goes
        // through Stripe Connect.
        var xendit = await tenantUow.Repository<PaymentGatewayConfiguration>()
            .GetAsync(g => g.Gateway == PaymentGateway.Xendit && g.IsActive, ct);
        if (xendit is not null)
        {
            return await CreateXenditInvoiceAsync(xendit, tenant, invoice, paymentLink, paymentAmount, req, ct);
        }

        if (string.IsNullOrEmpty(tenant.StripeConnectedAccountId))
        {
            return Result<PublicCheckoutSessionDto>.Fail("This company is not set up to receive payments.");
        }

        if (!tenant.ChargesEnabled)
        {
            return Result<PublicCheckoutSessionDto>.Fail(
                "This company cannot currently accept payments. Please contact them directly.");
        }

        try
        {
            var session = await stripeConnectService.CreateConnectedCheckoutSessionAsync(
                new CheckoutSessionRequest
                {
                    ConnectedAccountId = tenant.StripeConnectedAccountId,
                    Amount = new Money { Amount = paymentAmount, Currency = invoice.Total.Currency },
                    SuccessUrl = req.SuccessUrl,
                    CancelUrl = req.CancelUrl,
                    LineItemDescription = $"Invoice #{invoice.Number}",
                    Metadata = new Dictionary<string, string>
                    {
                        [StripeMetadataKeys.TenantId] = tenant.Id.ToString(),
                        ["invoice_id"] = invoice.Id.ToString(),
                        ["payment_link_token"] = paymentLink.Token
                    }
                });

            logger.LogInformation(
                "Created public Checkout Session {SessionId} for invoice {InvoiceId}",
                session.Id, invoice.Id);

            return Result<PublicCheckoutSessionDto>.Ok(new PublicCheckoutSessionDto { Url = session.Url });
        }
        catch (Stripe.StripeException ex)
        {
            logger.LogError(ex, "Failed to create public Checkout Session for invoice {InvoiceId}", invoice.Id);
            return Result<PublicCheckoutSessionDto>.Fail($"Failed to start payment: {ex.Message}");
        }
    }

    private async Task<Result<PublicCheckoutSessionDto>> CreateXenditInvoiceAsync(
        PaymentGatewayConfiguration xendit,
        Tenant tenant,
        Invoice invoice,
        PaymentLink paymentLink,
        decimal amount,
        CreatePublicCheckoutSessionCommand req,
        CancellationToken ct)
    {
        try
        {
            // The callback finds our invoice from external_id (INV{number}-{invoiceId:N}), after
            // re-reading the payment from Xendit. Retries share it; Xendit's own id tells them apart.
            var xenditInvoice = await xenditClient.CreateInvoiceAsync(xendit.SecretKey, new XenditInvoiceRequest
            {
                ExternalId = $"INV{invoice.Number}-{invoice.Id:N}",
                Amount = amount,
                Currency = invoice.Total.Currency,
                Description = $"{tenant.CompanyName ?? tenant.Name} - Invoice #{invoice.Number}",
                SuccessRedirectUrl = req.SuccessUrl,
                FailureRedirectUrl = req.CancelUrl,
                Duration = TimeSpan.FromHours(24),
                Metadata = new Dictionary<string, string>
                {
                    [XenditMetadataKeys.TenantId] = tenant.Id.ToString(),
                    [XenditMetadataKeys.InvoiceId] = invoice.Id.ToString(),
                    [XenditMetadataKeys.PaymentLinkId] = paymentLink.Id.ToString()
                }
            }, ct);

            logger.LogInformation(
                "Created Xendit invoice {XenditInvoiceId} for invoice {InvoiceId}",
                xenditInvoice.Id, invoice.Id);

            return Result<PublicCheckoutSessionDto>.Ok(new PublicCheckoutSessionDto { Url = xenditInvoice.InvoiceUrl });
        }
        catch (XenditApiException ex)
        {
            logger.LogError(ex, "Failed to create Xendit invoice for invoice {InvoiceId}", invoice.Id);
            return Result<PublicCheckoutSessionDto>.Fail($"Failed to start payment: {ex.Message}");
        }
    }
}
