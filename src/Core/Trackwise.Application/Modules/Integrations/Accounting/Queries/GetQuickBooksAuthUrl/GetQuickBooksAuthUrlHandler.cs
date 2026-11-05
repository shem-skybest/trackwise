using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Accounting;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Accounting.Queries;

internal sealed class GetQuickBooksAuthUrlHandler(
    ITenantUnitOfWork tenantUow,
    IAccountingProviderFactory providerFactory,
    IOAuthStateProtector stateProtector)
    : IAppRequestHandler<GetQuickBooksAuthUrlQuery, Result<AccountingAuthUrlDto>>
{
    public Task<Result<AccountingAuthUrlDto>> Handle(GetQuickBooksAuthUrlQuery req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();
        var state = stateProtector.Protect(tenant.Id);

        var provider = providerFactory.GetProvider(AccountingProviderType.QuickBooksOnline);
        var url = provider.BuildAuthorizationUrl(state);

        return Task.FromResult(Result<AccountingAuthUrlDto>.Ok(new AccountingAuthUrlDto { Url = url }));
    }
}
