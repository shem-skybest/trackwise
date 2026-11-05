using Trackwise.Infrastructure.Integrations.Common;
using Trackwise.Infrastructure.Integrations.LoadBoard.Credit;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.Dat;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.OneTwo3;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.Truckstop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Trackwise.Application.Abstractions.LoadBoard;

namespace Trackwise.Infrastructure.Integrations.LoadBoard;

public static class Registrar
{
    /// <summary>
    ///     Add LoadBoard provider integrations (DAT, Truckstop, 123Loadboard, Demo).
    /// </summary>
    public static IServiceCollection AddLoadBoardIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // LoadBoard providers (with HttpClient for external APIs)
        services.AddHttpClient<DatLoadBoardService>();
        services.AddHttpClient<TruckstopLoadBoardService>();
        services.AddHttpClient<OneTwo3LoadBoardService>();
        services.AddScoped<DemoLoadBoardService>();

        // Singleton: the typed-client service above is transient, counters would reset every resolution
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IOneTwo3SearchRateLimiter, InMemoryOneTwo3SearchRateLimiter>();

        // Options binding + factory pattern for provider selection
        services.AddProviderIntegration<LoadBoardOptions, ILoadBoardProviderFactory, LoadBoardProviderFactory>(
            configuration, LoadBoardOptions.SectionName);

        // Broker credit check (cache -> demo/FMCSA authority fallback)
        services.Configure<FmcsaOptions>(configuration.GetSection(FmcsaOptions.SectionName));
        services.AddHttpClient<FmcsaClient>();
        services.AddScoped<IBrokerCreditService, BrokerCreditService>();

        return services;
    }
}
