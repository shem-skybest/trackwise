using Trackwise.Application.Abstractions.FuelCards;
using Trackwise.Infrastructure.Integrations.Common;
using Trackwise.Infrastructure.Integrations.FuelCards.Providers;
using Trackwise.Infrastructure.Integrations.FuelCards.Providers.Efs;
using Trackwise.Infrastructure.Integrations.FuelCards.Providers.Wex;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Trackwise.Infrastructure.Integrations.FuelCards;

public static class Registrar
{
    /// <summary>
    ///     Add fuel card provider integrations (WEX, EFS, Demo).
    /// </summary>
    public static IServiceCollection AddFuelCardIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Fuel card providers (with HttpClient for external APIs)
        services.AddHttpClient<WexFuelCardService>();
        services.AddHttpClient<EfsFuelCardService>();
        services.AddScoped<DemoFuelCardService>();

        // Options binding + factory pattern for provider selection
        services.AddProviderIntegration<FuelCardsOptions, IFuelCardProviderFactory, FuelCardProviderFactory>(
            configuration, FuelCardsOptions.SectionName);

        return services;
    }
}
