using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Payments.Xendit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Webhooks.Commands;

/// <summary>
/// Records a payment when Xendit says an invoice was paid. The callback body is only a hint:
/// after the token check, the invoice is read back from Xendit with the tenant's own key and only
/// that answer is trusted. Xendit retries until it gets a 200, so a repeat is a no-op.
/// </summary>
internal sealed class ProcessXenditCallbackHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IXenditClient xenditClient,
    ILogger<ProcessXenditCallbackHandler> logger)
    : IAppRequestHandler<ProcessXenditCallbackCommand, Result>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public async Task<Result> Handle(ProcessXenditCallbackCommand req, CancellationToken ct)
    {
        var tenant = await masterUow.Repository<Tenant>().GetByIdAsync(req.TenantId, ct);
        if (tenant is null)
        {
            return Reject($"Xendit callback for unknown tenant {req.TenantId}");
        }

        tenantUow.SetCurrentTenant(tenant);

        // Not filtered on IsActive: a payment started before Xendit was switched off still has to land.
        var config = await tenantUow.Repository<PaymentGatewayConfiguration>()
            .GetAsync(g => g.Gateway == PaymentGateway.Xendit, ct);
        if (config is null || !TokensMatch(req.CallbackToken, config.CallbackToken))
        {
            return Reject($"Xendit callback token rejected for tenant {req.TenantId}");
        }

        var xenditInvoiceId = ReadInvoiceId(req.RawBody);
        if (xenditInvoiceId is null)
        {
            // Xendit's dashboard "Test" button and non-invoice products land here too.
            logger.LogInformation("Xendit callback without an invoice id ignored");
            return Result.Ok();
        }

        XenditInvoice xenditInvoice;
        try
        {
            xenditInvoice = await xenditClient.GetInvoiceAsync(config.SecretKey, xenditInvoiceId, ct);
        }
        catch (XenditApiException ex) when (ex.StatusCode == 404)
        {
            // The dashboard test callback sends a made-up id.
            logger.LogInformation("Xendit callback for invoice {XenditInvoiceId} that Xendit doesn't know; ignored", xenditInvoiceId);
            return Result.Ok();
        }
        catch (XenditApiException ex)
        {
            // Fail so Xendit retries later.
            return Result.Fail($"Could not confirm Xendit invoice {xenditInvoiceId}: {ex.Message}");
        }

        if (!xenditInvoice.IsPaid)
        {
            logger.LogInformation(
                "Xendit invoice {XenditInvoiceId} is {Status}; nothing to record",
                xenditInvoice.Id, xenditInvoice.Status);
            return Result.Ok();
        }

        if (!TryReadInvoiceGuid(xenditInvoice.ExternalId, out var invoiceId))
        {
            logger.LogWarning(
                "Paid Xendit invoice {XenditInvoiceId} has external id {ExternalId} that isn't ours; ignored",
                xenditInvoice.Id, xenditInvoice.ExternalId);
            return Result.Ok();
        }

        var existing = await tenantUow.Repository<Payment>().GetAsync(
            p => p.Gateway == PaymentGateway.Xendit && p.GatewayReference == xenditInvoice.Id, ct);
        if (existing is not null)
        {
            return Result.Ok();
        }

        var invoice = await tenantUow.Repository<Invoice>().GetByIdAsync(invoiceId, ct);
        if (invoice is null)
        {
            logger.LogError(
                "Paid Xendit invoice {XenditInvoiceId} points at invoice {InvoiceId}, which doesn't exist",
                xenditInvoice.Id, invoiceId);
            return Result.Ok();
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
        return Result.Ok();
    }

    private Result Reject(string message)
    {
        logger.LogWarning("{Message}", message);
        return Result.Fail(message, ErrorCodes.WebhookRejected);
    }

    private static bool TokensMatch(string? provided, string expected)
    {
        if (string.IsNullOrEmpty(provided))
        {
            return false;
        }

        var a = Encoding.UTF8.GetBytes(provided);
        var b = Encoding.UTF8.GetBytes(expected);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string? ReadInvoiceId(string rawBody)
    {
        try
        {
            return JsonSerializer.Deserialize<CallbackBody>(rawBody, JsonOptions)?.Id is { Length: > 0 } id
                ? id
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Our external id is <c>INV{number}-{invoiceId:N}</c>
    /// (see <c>CreatePublicCheckoutSessionHandler</c>).
    /// </summary>
    internal static bool TryReadInvoiceGuid(string externalId, out Guid invoiceId)
    {
        var dash = externalId.LastIndexOf('-');
        invoiceId = Guid.Empty;
        return externalId.StartsWith("INV", StringComparison.Ordinal)
               && dash > 0
               && Guid.TryParseExact(externalId[(dash + 1)..], "N", out invoiceId);
    }

    private sealed record CallbackBody
    {
        public string? Id { get; init; }
    }
}
