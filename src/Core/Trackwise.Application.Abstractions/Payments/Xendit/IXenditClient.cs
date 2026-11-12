namespace Trackwise.Application.Abstractions.Payments.Xendit;

/// <summary>
/// Xendit Invoice API (hosted payment page). Every call takes the tenant's own secret key -
/// money lands in the tenant's Xendit account, not a platform account.
/// </summary>
public interface IXenditClient
{
    /// <summary>
    /// Creates a hosted Xendit invoice. The customer pays on <see cref="XenditInvoice.InvoiceUrl"/>
    /// with whatever channels the account has switched on (cards, GCash, Maya, bank transfer...).
    /// </summary>
    /// <exception cref="XenditApiException">Xendit refused the request or could not be reached.</exception>
    Task<XenditInvoice> CreateInvoiceAsync(
        string secretKey, XenditInvoiceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Reads an invoice back from Xendit - the source of truth for whether it was paid.
    /// </summary>
    /// <exception cref="XenditApiException">Xendit refused the request or could not be reached.</exception>
    Task<XenditInvoice> GetInvoiceAsync(
        string secretKey, string invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Every Xendit invoice created with this external id, newest first - each try at paying one
    /// TrackWise invoice shares the external id.
    /// </summary>
    /// <exception cref="XenditApiException">Xendit refused the request or could not be reached.</exception>
    Task<IReadOnlyList<XenditInvoice>> ListInvoicesAsync(
        string secretKey, string externalId, CancellationToken ct = default);

    /// <summary>
    /// Checks a secret key against Xendit. Returns null when it works, or the reason it doesn't.
    /// </summary>
    Task<string?> ValidateSecretKeyAsync(string secretKey, CancellationToken ct = default);
}

public sealed record XenditInvoiceRequest
{
    /// <summary>Our own reference, echoed back on the callback.</summary>
    public required string ExternalId { get; init; }

    public required decimal Amount { get; init; }

    /// <summary>ISO currency, e.g. PHP. The Xendit account must have it enabled.</summary>
    public required string Currency { get; init; }

    public required string Description { get; init; }
    public string? SuccessRedirectUrl { get; init; }
    public string? FailureRedirectUrl { get; init; }
    public string? PayerEmail { get; init; }

    /// <summary>How long the hosted page stays payable.</summary>
    public TimeSpan? Duration { get; init; }

    public IDictionary<string, string>? Metadata { get; init; }
}

public sealed record XenditInvoice
{
    public required string Id { get; init; }
    public required string ExternalId { get; init; }

    /// <summary>PENDING, PAID, SETTLED or EXPIRED.</summary>
    public required string Status { get; init; }

    public required decimal Amount { get; init; }
    public decimal? PaidAmount { get; init; }
    public required string Currency { get; init; }
    public required string InvoiceUrl { get; init; }
    public string? PaymentMethod { get; init; }
    public string? PaymentChannel { get; init; }
    public DateTime? PaidAt { get; init; }

    /// <summary>PAID or SETTLED - the money was taken.</summary>
    public bool IsPaid => Status is "PAID" or "SETTLED";
}

public sealed class XenditApiException(string message, int? statusCode = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}
