using Trackwise.Application.Abstractions.FuelCards;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Integrations.Common;
using Trackwise.Infrastructure.Integrations.FuelCards.Providers;
using Trackwise.Infrastructure.Integrations.FuelCards.Providers.Efs;
using Trackwise.Infrastructure.Integrations.FuelCards.Providers.Wex;
using Microsoft.Extensions.Logging;

namespace Trackwise.Infrastructure.Integrations.FuelCards;

internal sealed class FuelCardProviderFactory(
    IServiceProvider serviceProvider,
    ILogger<FuelCardProviderFactory> logger)
    : ProviderFactoryBase<IFuelCardProviderService, FuelCardProviderType>(serviceProvider, logger),
        IFuelCardProviderFactory
{
    protected override string FamilyName => "Fuel card";

    protected override IReadOnlyDictionary<FuelCardProviderType, Type> Providers => ProviderMap;

    private static readonly IReadOnlyDictionary<FuelCardProviderType, Type> ProviderMap =
        new Dictionary<FuelCardProviderType, Type>
        {
            [FuelCardProviderType.Wex] = typeof(WexFuelCardService),
            [FuelCardProviderType.Efs] = typeof(EfsFuelCardService),
            [FuelCardProviderType.Demo] = typeof(DemoFuelCardService)
        };

    public IFuelCardProviderService GetProvider(FuelCardProviderConfiguration configuration)
    {
        var service = GetProvider(configuration.ProviderType);
        service.Initialize(configuration);
        return service;
    }
}
