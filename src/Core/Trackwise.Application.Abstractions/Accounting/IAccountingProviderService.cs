using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Abstractions.Accounting;

/// <summary>
/// Port for an accounting provider integration (e.g. QuickBooks Online). One-way push:
/// the TMS is the source of truth and creates/updates records in the provider.
/// </summary>
public interface IAccountingProviderService
{
    /// <summary>The provider this service handles.</summary>
    AccountingProviderType ProviderType { get; }

    /// <summary>Initialize the service with an existing tenant connection (tokens, realm).</summary>
    void Initialize(AccountingProviderConfiguration configuration);

    /// <summary>
    /// Build the provider's OAuth2 authorization URL to which the user is redirected to grant
    /// access. The redirect URI is taken from the provider's own configuration.
    /// </summary>
    string BuildAuthorizationUrl(string state);

    /// <summary>
    /// Exchange an OAuth2 authorization code (from the redirect callback) for tokens and the
    /// connected company details.
    /// </summary>
    Task<AccountingConnectionResultDto> ExchangeCodeAsync(
        string code, string realmId, CancellationToken ct = default);

    /// <summary>Refresh an access token using a refresh token; returns the rotated tokens.</summary>
    Task<OAuthTokenResultDto?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revoke the connection at the provider (called on disconnect).</summary>
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Fetch the connected company's Chart of Accounts (to resolve default accounts).</summary>
    Task<IReadOnlyList<QboAccountDto>> GetAccountsAsync(CancellationToken ct = default);

    /// <summary>Create or update a customer. Pass the existing external id/sync-token to update.</summary>
    Task<QboUpsertResult> UpsertCustomerAsync(
        QboCustomerPayload payload, string? qboId, string? syncToken, CancellationToken ct = default);

    /// <summary>Create or update an invoice.</summary>
    Task<QboUpsertResult> UpsertInvoiceAsync(
        QboInvoicePayload payload, string? qboId, string? syncToken, CancellationToken ct = default);

    /// <summary>Create or update a payment linked to a pushed invoice.</summary>
    Task<QboUpsertResult> UpsertPaymentAsync(
        QboPaymentPayload payload, string? qboId, string? syncToken, CancellationToken ct = default);

    /// <summary>Create or update an expense (QBO Purchase).</summary>
    Task<QboUpsertResult> UpsertExpenseAsync(
        QboExpensePayload payload, string? qboId, string? syncToken, CancellationToken ct = default);
}
