using Trackwise.Infrastructure.Integrations.Common;
using Trackwise.Infrastructure.Integrations.Eld.Providers;
using Trackwise.Infrastructure.Integrations.Eld.Providers.Geotab;
using Trackwise.Infrastructure.Integrations.Eld.Providers.Motive;
using Trackwise.Infrastructure.Integrations.Eld.Providers.Samsara;
using Trackwise.Infrastructure.Integrations.Eld.Providers.TtEld;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trackwise.Application.Abstractions.Eld;

namespace Trackwise.Infrastructure.Integrations.Eld;

public static class Registrar
{
    /// <summary>
    ///     Add ELD provider integrations (Samsara, Motive, TT ELD, Demo).
    /// </summary>
    public static IServiceCollection AddEldIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ELD providers (with HttpClient for external APIs)
        services.AddHttpClient<SamsaraEldService>();
        services.AddHttpClient<MotiveEldService>();
        services.AddHttpClient<TtEldService>();
        services.AddHttpClient<GeotabClient>();
        services.AddScoped<GeotabEldService>();
        services.AddScoped<DemoEldService>();

        // Options binding + factory pattern for provider selection
        services.AddProviderIntegration<EldOptions, IEldProviderFactory, EldProviderFactory>(
            configuration, EldOptions.SectionName);
        return services;
    }
}
