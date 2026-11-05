using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Integrations.Common;
using Trackwise.Infrastructure.Integrations.Eld.Providers;
using Trackwise.Infrastructure.Integrations.Eld.Providers.Geotab;
using Trackwise.Infrastructure.Integrations.Eld.Providers.Motive;
using Trackwise.Infrastructure.Integrations.Eld.Providers.Samsara;
using Trackwise.Infrastructure.Integrations.Eld.Providers.TtEld;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Eld;

namespace Trackwise.Infrastructure.Integrations.Eld;

internal sealed class EldProviderFactory(
    IServiceProvider serviceProvider,
    ILogger<EldProviderFactory> logger)
    : ProviderFactoryBase<IEldProviderService, EldProviderType>(serviceProvider, logger), IEldProviderFactory
{
    protected override string FamilyName => "ELD";

    protected override IReadOnlyDictionary<EldProviderType, Type> Providers => ProviderMap;

    private static readonly IReadOnlyDictionary<EldProviderType, Type> ProviderMap =
        new Dictionary<EldProviderType, Type>
        {
            [EldProviderType.Samsara] = typeof(SamsaraEldService),
            [EldProviderType.Motive] = typeof(MotiveEldService),
            [EldProviderType.TtEld] = typeof(TtEldService),
            [EldProviderType.Demo] = typeof(DemoEldService),
            [EldProviderType.Geotab] = typeof(GeotabEldService)
        };

    public IEldProviderService GetProvider(EldProviderConfiguration configuration)
    {
        var service = GetProvider(configuration.ProviderType);
        service.Initialize(configuration);
        return service;
    }
}
