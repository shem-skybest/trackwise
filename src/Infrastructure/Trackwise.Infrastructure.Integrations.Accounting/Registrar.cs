using Trackwise.Application.Abstractions.Accounting;
using Trackwise.Infrastructure.Integrations.Accounting.Providers;
using Trackwise.Infrastructure.Integrations.Accounting.Providers.QuickBooks;
using Trackwise.Infrastructure.Integrations.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trackwise.Infrastructure.Integrations.Accounting;

public static class Registrar
{
    /// <summary>
    ///     Add accounting provider integrations (QuickBooks Online, Demo).
    /// </summary>
    public static IServiceCollection AddAccountingIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpClient<QuickBooksOAuthClient>();
        services.AddHttpClient<QuickBooksOnlineService>();
        services.AddScoped<DemoAccountingService>();

        services.AddProviderIntegration<AccountingOptions, IAccountingProviderFactory, AccountingProviderFactory>(
            configuration, AccountingOptions.SectionName);
        return services;
    }
}
