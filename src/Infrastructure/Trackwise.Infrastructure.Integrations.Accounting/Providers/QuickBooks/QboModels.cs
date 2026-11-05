using System.Text.Json.Serialization;

namespace Trackwise.Infrastructure.Integrations.Accounting.Providers.QuickBooks;

// --- OAuth ---

internal sealed record QboTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("x_refresh_token_expires_in")] public int RefreshTokenExpiresIn { get; set; }
}

// --- Reference type (QBO uses lowercase "value"/"name") ---

internal sealed record QboRef
{
    [JsonPropertyName("value")] public string? Value { get; set; }

    public static QboRef Of(string value) => new() { Value = value };
}

// --- Common envelope pieces ---

internal sealed record QboEmail
{
    public string? Address { get; set; }
}

internal sealed record QboPhone
{
    public string? FreeFormNumber { get; set; }
}

internal sealed record QboPhysicalAddress
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? CountrySubDivisionCode { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

// --- Customer ---

internal sealed record QboCustomer
{
    public string? Id { get; set; }
    public string? SyncToken { get; set; }
    public string? DisplayName { get; set; }
    public QboEmail? PrimaryEmailAddr { get; set; }
    public QboPhone? PrimaryPhone { get; set; }
    public QboPhysicalAddress? BillAddr { get; set; }
}

internal sealed record QboCustomerResponse
{
    public QboCustomer? Customer { get; set; }
}

// --- Invoice ---

internal sealed record QboLine
{
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? DetailType { get; set; }
    public QboSalesItemLineDetail? SalesItemLineDetail { get; set; }
    public QboAccountBasedExpenseLineDetail? AccountBasedExpenseLineDetail { get; set; }
    public List<QboLinkedTxn>? LinkedTxn { get; set; }
}

internal sealed record QboSalesItemLineDetail
{
    public decimal? Qty { get; set; }
    public decimal? UnitPrice { get; set; }
    public QboRef? ItemRef { get; set; }
}

internal sealed record QboAccountBasedExpenseLineDetail
{
    public QboRef? AccountRef { get; set; }
}

internal sealed record QboLinkedTxn
{
    public string? TxnId { get; set; }
    public string? TxnType { get; set; }
}

internal sealed record QboTxnTaxDetail
{
    public decimal? TotalTax { get; set; }
}

internal sealed record QboInvoice
{
    public string? Id { get; set; }
    public string? SyncToken { get; set; }
    public string? DocNumber { get; set; }
    public QboRef? CustomerRef { get; set; }
    public QboRef? CurrencyRef { get; set; }
    public string? DueDate { get; set; }
    public QboTxnTaxDetail? TxnTaxDetail { get; set; }
    public List<QboLine> Line { get; set; } = [];
}

internal sealed record QboInvoiceResponse
{
    public QboInvoice? Invoice { get; set; }
}

// --- Payment ---

internal sealed record QboPayment
{
    public string? Id { get; set; }
    public string? SyncToken { get; set; }
    public QboRef? CustomerRef { get; set; }
    public QboRef? CurrencyRef { get; set; }
    public decimal TotalAmt { get; set; }
    public string? PaymentRefNum { get; set; }
    public List<QboLine> Line { get; set; } = [];
}

internal sealed record QboPaymentResponse
{
    public QboPayment? Payment { get; set; }
}

// --- Purchase (expense) ---

internal sealed record QboPurchase
{
    public string? Id { get; set; }
    public string? SyncToken { get; set; }
    public QboRef? AccountRef { get; set; }
    public QboRef? CurrencyRef { get; set; }
    public string? PaymentType { get; set; }
    public decimal TotalAmt { get; set; }
    public string? TxnDate { get; set; }
    public List<QboLine> Line { get; set; } = [];
}

internal sealed record QboPurchaseResponse
{
    public QboPurchase? Purchase { get; set; }
}

// --- Account (Chart of Accounts) ---

internal sealed record QboAccount
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? AccountType { get; set; }
    public string? Classification { get; set; }
}

// --- Query responses ---

internal sealed record QboQueryResponse<T>
{
    public QboQueryEnvelope<T>? QueryResponse { get; set; }
}

internal sealed record QboQueryEnvelope<T>
{
    public List<T>? Account { get; set; }
    public List<T>? CompanyInfo { get; set; }
}

internal sealed record QboCompanyInfo
{
    public string? CompanyName { get; set; }
}
