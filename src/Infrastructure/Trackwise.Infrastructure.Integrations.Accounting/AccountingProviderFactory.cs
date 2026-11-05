using Trackwise.Application.Abstractions.Accounting;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Integrations.Accounting.Providers;
using Trackwise.Infrastructure.Integrations.Accounting.Providers.QuickBooks;
using Trackwise.Infrastructure.Integrations.Common;
using Microsoft.Extensions.Logging;

namespace Trackwise.Infrastructure.Integrations.Accounting;

internal sealed class AccountingProviderFactory(
    IServiceProvider serviceProvider,
    ILogger<AccountingProviderFactory> logger)
    : ProviderFactoryBase<IAccountingProviderService, AccountingProviderType>(serviceProvider, logger),
        IAccountingProviderFactory
{
    protected override string FamilyName => "Accounting";

    protected override IReadOnlyDictionary<AccountingProviderType, Type> Providers => ProviderMap;

    private static readonly IReadOnlyDictionary<AccountingProviderType, Type> ProviderMap =
        new Dictionary<AccountingProviderType, Type>
        {
            [AccountingProviderType.QuickBooksOnline] = typeof(QuickBooksOnlineService),
            [AccountingProviderType.Demo] = typeof(DemoAccountingService)
        };

    public IAccountingProviderService GetProvider(AccountingProviderConfiguration configuration)
    {
        var service = GetProvider(configuration.ProviderType);
        service.Initialize(configuration);
        return service;
    }
}
