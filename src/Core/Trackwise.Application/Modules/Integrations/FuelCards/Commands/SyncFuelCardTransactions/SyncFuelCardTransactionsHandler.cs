using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Integrations.FuelCards.Services;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Commands;

internal sealed class SyncFuelCardTransactionsHandler(IFuelCardSyncService syncService)
    : IAppRequestHandler<SyncFuelCardTransactionsCommand, Result<FuelCardSyncResultDto>>
{
    public async Task<Result<FuelCardSyncResultDto>> Handle(SyncFuelCardTransactionsCommand req, CancellationToken ct)
    {
        var result = await syncService.SyncCurrentTenantAsync(req.ProviderType, ct);
        return Result<FuelCardSyncResultDto>.Ok(result);
    }
}
