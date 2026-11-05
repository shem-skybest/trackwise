using Trackwise.Infrastructure.Routing.Ferry;
using Trackwise.Infrastructure.Routing.Geocoding;
using Trackwise.Infrastructure.Routing.Geospatial;
using Trackwise.Infrastructure.Routing.Optimization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trackwise.Application.Abstractions.Geocoding;
using Trackwise.Application.Abstractions.Routing;

namespace Trackwise.Infrastructure.Routing;

public static class Registrar
{
    public static IServiceCollection AddRoutingInfrastructure(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Geocoding services
        services.Configure<MapboxOptions>(configuration.GetSection(MapboxOptions.SectionName));
        services.AddHttpClient<IGeocodingService, MapboxGeocodingService>();
        services.AddHttpClient<MapboxMatrixClient>();

        // Offline point-in-polygon jurisdiction lookup (IFTA mileage attribution)
        services.AddSingleton<IJurisdictionResolver, JurisdictionResolver>();
        services.AddHostedService<JurisdictionResolverWarmup>();

        // PH inter-island drive+ferry routing (Mapbox has no ferry-aware profile)
        services.AddSingleton<IFerryRouteResolver, FerryRouteResolver>();

        // Trip optimization services
        services.AddSingleton<HeuristicTripOptimizer>();
        services.AddSingleton<MapboxMatrixTripOptimizer>();
        services.AddSingleton<ITripOptimizer, CompositeTripOptimizer>();
        return services;
    }
}
