using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>
/// Connection status for a tenant's accounting provider, surfaced to the settings UI.
/// </summary>
public record AccountingConnectionDto
{
    public AccountingProviderType ProviderType { get; set; }
    public bool IsConnected { get; set; }
    public string? CompanyName { get; set; }
    public string? RealmId { get; set; }
    public DateTime? LastSyncedAt { get; set; }
}

/// <summary>
/// The provider OAuth authorization URL the frontend should redirect the browser to.
/// </summary>
public record AccountingAuthUrlDto
{
    public required string Url { get; set; }
}

/// <summary>
/// Result of exchanging an OAuth authorization code for tokens, plus the connected company.
/// </summary>
public record AccountingConnectionResultDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
    public required string RealmId { get; set; }
    public string? CompanyName { get; set; }
}

/// <summary>
/// Provider-neutral result of an upsert: the external ID and its new concurrency token.
/// </summary>
public record QboUpsertResult
{
    public required string QboId { get; set; }
    public string? SyncToken { get; set; }
}

/// <summary>
/// A Chart-of-Accounts entry from the provider, used to resolve default expense/payment accounts.
/// </summary>
public record QboAccountDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? AccountType { get; set; }
    public string? Classification { get; set; }
}

/// <summary>Provider-neutral customer push payload.</summary>
public record QboCustomerPayload
{
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Address? BillingAddress { get; set; }
}

/// <summary>A single invoice line for a push.</summary>
public record QboInvoiceLinePayload
{
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public int Quantity { get; set; } = 1;
}

/// <summary>Provider-neutral invoice push payload.</summary>
public record QboInvoicePayload
{
    public required string QboCustomerId { get; set; }
    public string? DocNumber { get; set; }
    public required string CurrencyCode { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TaxTotal { get; set; }
    public List<QboInvoiceLinePayload> Lines { get; set; } = [];
}

/// <summary>Provider-neutral payment push payload, linked to a pushed invoice.</summary>
public record QboPaymentPayload
{
    public required string QboCustomerId { get; set; }
    public required string QboInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public required string CurrencyCode { get; set; }
    public string? ReferenceNumber { get; set; }
}

/// <summary>Provider-neutral expense (QBO Purchase) push payload.</summary>
public record QboExpensePayload
{
    public required string PaymentAccountId { get; set; }
    public required string ExpenseAccountId { get; set; }
    public decimal Amount { get; set; }
    public required string CurrencyCode { get; set; }
    public DateTime TxnDate { get; set; }
    public string? Description { get; set; }
}
