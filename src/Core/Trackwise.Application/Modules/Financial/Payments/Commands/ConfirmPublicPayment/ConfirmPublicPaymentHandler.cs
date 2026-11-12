using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Payments.Xendit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

internal sealed class ConfirmPublicPaymentHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IXenditClient xenditClient,
    ILogger<ConfirmPublicPaymentHandler> logger)
    : IAppRequestHandler<ConfirmPublicPaymentCommand, Result<PublicPaymentConfirmationDto>>
{
    public async Task<Result<PublicPaymentConfirmationDto>> Handle(
        ConfirmPublicPaymentCommand req, CancellationToken ct)
    {
        var tenant = await masterUow.Repository<Tenant>().GetByIdAsync(req.TenantId, ct);
        if (tenant is null)
        {
            return Result<PublicPaymentConfirmationDto>.Fail("Invalid payment link.");
        }

        tenantUow.SetCurrentTenant(tenant);

        var paymentLink = await tenantUow.Repository<PaymentLink>()
            .GetAsync(p => p.Token == req.Token, ct);
        if (paymentLink is null || !paymentLink.IsValid)
        {
            return Result<PublicPaymentConfirmationDto>.Fail("This payment link has expired or been revoked.");
        }

        var invoice = await tenantUow.Repository<Invoice>().GetByIdAsync(paymentLink.InvoiceId, ct);
        if (invoice is null)
        {
            return Result<PublicPaymentConfirmationDto>.Fail("Invoice not found.");
        }

        // Not filtered on IsActive, same as the callback: a payment started before Xendit was
        // switched off still has to land.
        var xendit = await tenantUow.Repository<PaymentGatewayConfiguration>()
            .GetAsync(g => g.Gateway == PaymentGateway.Xendit, ct);
        if (xendit is null)
        {
            // Stripe settles through its own webhook; nothing to check here.
            return Confirmation(PublicPaymentState.NotChecked);
        }

        IReadOnlyList<XenditInvoice> attempts;
        try
        {
            attempts = await xenditClient.ListInvoicesAsync(
                xendit.SecretKey, XenditPayments.ExternalIdFor(invoice), ct);
        }
        catch (XenditApiException ex)
        {
            logger.LogWarning(ex, "Could not check Xendit for invoice {InvoiceId}", invoice.Id);
            return Confirmation(PublicPaymentState.Pending);
        }

        var anyPaid = false;
        foreach (var attempt in attempts.Where(a => a.IsPaid))
        {
            anyPaid = true;
            try
            {
                await XenditPayments.RecordIfPaidAsync(tenantUow, tenant, attempt, logger, ct);
            }
            catch (DbUpdateException ex)
            {
                // The callback recorded the same Xendit invoice a moment earlier (unique on
                // gateway + reference). It's paid either way.
                logger.LogInformation(ex,
                    "Xendit invoice {XenditInvoiceId} was recorded by the callback first", attempt.Id);
                break;
            }
        }

        if (anyPaid)
        {
            return Confirmation(PublicPaymentState.Paid);
        }

        return Confirmation(attempts.Any(a => a.Status == "PENDING")
            ? PublicPaymentState.Pending
            : PublicPaymentState.NotPaid);
    }

    private static Result<PublicPaymentConfirmationDto> Confirmation(PublicPaymentState state) =>
        Result<PublicPaymentConfirmationDto>.Ok(new PublicPaymentConfirmationDto { State = state });
}
