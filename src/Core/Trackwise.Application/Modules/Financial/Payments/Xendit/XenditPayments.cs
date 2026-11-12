using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Payments.Xendit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Financial.Payments;

/// <summary>
/// Turns a Xendit invoice, as Xendit itself reports it, into a TrackWise payment. Shared by the
/// "Invoices paid" callback and the pay page's return check, so whichever arrives first records
/// it and the other finds it already there.
/// </summary>
internal static class XenditPayments
{
    /// <summary>
    /// Our Xendit external id, <c>INV{number}-{invoiceId:N}</c>. Every attempt to pay one invoice
    /// shares it, which is what lets the return check list them all; Xendit's own id tells them apart.
    /// </summary>
    public static string ExternalIdFor(Invoice invoice) => $"INV{invoice.Number}-{invoice.Id:N}";

    public static bool TryReadInvoiceId(string externalId, out Guid invoiceId)
    {
        var dash = externalId.LastIndexOf('-');
        invoiceId = Guid.Empty;
        return externalId.StartsWith("INV", StringComparison.Ordinal)
               && dash > 0
               && Guid.TryParseExact(externalId[(dash + 1)..], "N", out invoiceId);
    }

    /// <summary>
    /// Records the payment when Xendit says the invoice was paid and it isn't recorded yet.
    /// Saves the tenant unit of work when it records.
    /// </summary>
    public static async Task<XenditRecordOutcome> RecordIfPaidAsync(
        ITenantUnitOfWork tenantUow,
        Tenant tenant,
        XenditInvoice xenditInvoice,
        ILogger logger,
        CancellationToken ct)
    {
        if (!xenditInvoice.IsPaid)
        {
            logger.LogInformation(
                "Xendit invoice {XenditInvoiceId} is {Status}; nothing to record",
                xenditInvoice.Id, xenditInvoice.Status);
            return XenditRecordOutcome.NotPaid;
        }

        if (!TryReadInvoiceId(xenditInvoice.ExternalId, out var invoiceId))
        {
            logger.LogWarning(
                "Paid Xendit invoice {XenditInvoiceId} has external id {ExternalId} that isn't ours; ignored",
                xenditInvoice.Id, xenditInvoice.ExternalId);
            return XenditRecordOutcome.NotOurs;
        }

        var existing = await tenantUow.Repository<Payment>().GetAsync(
            p => p.Gateway == PaymentGateway.Xendit && p.GatewayReference == xenditInvoice.Id, ct);
        if (existing is not null)
        {
            return XenditRecordOutcome.AlreadyRecorded;
        }

        var invoice = await tenantUow.Repository<Invoice>().GetByIdAsync(invoiceId, ct);
        if (invoice is null)
        {
            logger.LogError(
                "Paid Xendit invoice {XenditInvoiceId} points at invoice {InvoiceId}, which doesn't exist",
                xenditInvoice.Id, invoiceId);
            return XenditRecordOutcome.InvoiceMissing;
        }

        var channel = xenditInvoice.PaymentChannel ?? xenditInvoice.PaymentMethod;
        var payment = new Payment
        {
            Amount = new Money
            {
                Amount = xenditInvoice.PaidAmount ?? xenditInvoice.Amount,
                Currency = xenditInvoice.Currency.ToUpperInvariant()
            },
            Status = PaymentStatus.Paid,
            TenantId = tenant.Id,
            Gateway = PaymentGateway.Xendit,
            GatewayReference = xenditInvoice.Id,
            ReferenceNumber = xenditInvoice.Id,
            // Xendit doesn't give a billing address; same placeholder the Stripe path uses.
            BillingAddress = tenant.CompanyAddress,
            Description = channel is null
                ? $"Xendit payment for Invoice #{invoice.Number}"
                : $"Xendit {channel} payment for Invoice #{invoice.Number}"
        };

        await tenantUow.Repository<Payment>().AddAsync(payment, ct);
        invoice.ApplyPayment(payment);
        tenantUow.Repository<Invoice>().Update(invoice);
        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded Xendit payment {PaymentId} ({Amount} {Currency}, {Channel}) for invoice {InvoiceId}",
            payment.Id, payment.Amount.Amount, payment.Amount.Currency, channel, invoice.Id);
        return XenditRecordOutcome.Recorded;
    }
}

internal enum XenditRecordOutcome
{
    Recorded,
    AlreadyRecorded,
    NotPaid,
    NotOurs,
    InvoiceMissing
}
