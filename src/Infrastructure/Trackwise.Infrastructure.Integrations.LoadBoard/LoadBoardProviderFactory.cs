using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Infrastructure.Integrations.Common;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.Dat;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.OneTwo3;
using Trackwise.Infrastructure.Integrations.LoadBoard.Providers.Truckstop;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.LoadBoard;

namespace Trackwise.Infrastructure.Integrations.LoadBoard;

internal sealed class LoadBoardProviderFactory(
    IServiceProvider serviceProvider,
    ILogger<LoadBoardProviderFactory> logger)
    : ProviderFactoryBase<ILoadBoardProviderService, LoadBoardProviderType>(serviceProvider, logger),
        ILoadBoardProviderFactory
{
    protected override string FamilyName => "Load board";

    protected override IReadOnlyDictionary<LoadBoardProviderType, Type> Providers => ProviderMap;

    private static readonly IReadOnlyDictionary<LoadBoardProviderType, Type> ProviderMap =
        new Dictionary<LoadBoardProviderType, Type>
        {
            [LoadBoardProviderType.Dat] = typeof(DatLoadBoardService),
            [LoadBoardProviderType.Truckstop] = typeof(TruckstopLoadBoardService),
            [LoadBoardProviderType.OneTwo3Loadboard] = typeof(OneTwo3LoadBoardService),
            [LoadBoardProviderType.Demo] = typeof(DemoLoadBoardService)
        };

    public ILoadBoardProviderService GetProvider(LoadBoardConfiguration configuration)
    {
        var service = GetProvider(configuration.ProviderType);
        service.Initialize(configuration);
        return service;
    }
}
