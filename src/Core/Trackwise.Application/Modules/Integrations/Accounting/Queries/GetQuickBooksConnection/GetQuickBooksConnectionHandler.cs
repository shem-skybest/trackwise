using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Accounting.Queries;

internal sealed class GetQuickBooksConnectionHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetQuickBooksConnectionQuery, Result<AccountingConnectionDto>>
{
    public async Task<Result<AccountingConnectionDto>> Handle(GetQuickBooksConnectionQuery req, CancellationToken ct)
    {
        var config = await tenantUow.Repository<AccountingProviderConfiguration>()
            .GetAsync(c => c.ProviderType == AccountingProviderType.QuickBooksOnline, ct);

        var dto = new AccountingConnectionDto
        {
            ProviderType = AccountingProviderType.QuickBooksOnline,
            IsConnected = config is { IsActive: true, AccessToken: not null },
            CompanyName = config?.CompanyName,
            RealmId = config?.RealmId,
            LastSyncedAt = config?.LastSyncedAt
        };

        return Result<AccountingConnectionDto>.Ok(dto);
    }
}
