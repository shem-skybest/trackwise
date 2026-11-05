using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Per-tenant configuration for an accounting provider integration (e.g. QuickBooks Online).
/// Holds the OAuth2 connection and sync bookkeeping for one provider.
/// </summary>
public class AccountingProviderConfiguration : Entity, ITenantEntity
{
    public required AccountingProviderType ProviderType { get; set; }

    /// <summary>
    /// The accounting company/realm ID in the provider's system (QBO "realmId").
    /// </summary>
    public string? RealmId { get; set; }

    /// <summary>
    /// Human-readable company name from the provider, for display in settings.
    /// </summary>
    public string? CompanyName { get; set; }

    /// <summary>
    /// OAuth2 access token (encrypted at rest).
    /// </summary>
    [EncryptedSecret]
    public string? AccessToken { get; set; }

    /// <summary>
    /// OAuth2 refresh token used to renew the access token (encrypted at rest).
    /// </summary>
    [EncryptedSecret]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// When the current access token expires.
    /// </summary>
    public DateTime? TokenExpiresAt { get; set; }

    /// <summary>
    /// When the refresh token expires (QBO refresh tokens last ~100 days).
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>
    /// QBO account ID used as the payment/bank account when pushing expenses (Purchase.AccountRef).
    /// Resolved from the tenant's Chart of Accounts on connect.
    /// </summary>
    public string? DefaultPaymentAccountId { get; set; }

    /// <summary>
    /// QBO account ID used as the expense account on pushed expense lines when no
    /// category-specific mapping is available.
    /// </summary>
    public string? DefaultExpenseAccountId { get; set; }

    /// <summary>
    /// Whether this provider configuration is active (a disconnect deactivates it).
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When data was last pushed to this provider.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
}
