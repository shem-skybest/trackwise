using Trackwise.Application.Abstractions.Accounting;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Infrastructure.Integrations.Accounting.Providers;

/// <summary>
///     No-op accounting provider for demo tenants. Echoes deterministic fake IDs so the sync
///     pipeline can be exercised without a real QuickBooks connection.
/// </summary>
internal sealed class DemoAccountingService : IAccountingProviderService
{
    public AccountingProviderType ProviderType => AccountingProviderType.Demo;

    public void Initialize(AccountingProviderConfiguration configuration)
    {
    }

    public string BuildAuthorizationUrl(string state) => $"about:blank#demo-{state}";

    public Task<AccountingConnectionResultDto> ExchangeCodeAsync(
        string code, string realmId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return Task.FromResult(new AccountingConnectionResultDto
        {
            AccessToken = "demo-access-token",
            RefreshToken = "demo-refresh-token",
            AccessTokenExpiresAt = now.AddHours(1),
            RefreshTokenExpiresAt = now.AddDays(100),
            RealmId = string.IsNullOrEmpty(realmId) ? "demo-realm" : realmId,
            CompanyName = "Demo Company"
        });
    }

    public Task<OAuthTokenResultDto?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
        Task.FromResult<OAuthTokenResultDto?>(new OAuthTokenResultDto
        {
            AccessToken = "demo-access-token",
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });

    public Task RevokeAsync(string refreshToken, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<QboAccountDto>> GetAccountsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<QboAccountDto>>(
        [
            new QboAccountDto { Id = "35", Name = "Checking", AccountType = "Bank", Classification = "Asset" },
            new QboAccountDto { Id = "60", Name = "Job Expenses", AccountType = "Expense", Classification = "Expense" }
        ]);

    public Task<QboUpsertResult> UpsertCustomerAsync(
        QboCustomerPayload payload, string? qboId, string? syncToken, CancellationToken ct = default) =>
        Fake(qboId);

    public Task<QboUpsertResult> UpsertInvoiceAsync(
        QboInvoicePayload payload, string? qboId, string? syncToken, CancellationToken ct = default) =>
        Fake(qboId);

    public Task<QboUpsertResult> UpsertPaymentAsync(
        QboPaymentPayload payload, string? qboId, string? syncToken, CancellationToken ct = default) =>
        Fake(qboId);

    public Task<QboUpsertResult> UpsertExpenseAsync(
        QboExpensePayload payload, string? qboId, string? syncToken, CancellationToken ct = default) =>
        Fake(qboId);

    private static Task<QboUpsertResult> Fake(string? qboId) =>
        Task.FromResult(new QboUpsertResult
        {
            QboId = qboId ?? Guid.NewGuid().ToString("N")[..8],
            SyncToken = "0"
        });
}
