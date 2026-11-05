using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Commands;

/// <summary>
/// Manually triggers a fuel card transaction sync for the current tenant
/// (the nightly Hangfire job runs the same logic).
/// </summary>
[RequiresFeature(TenantFeature.FuelCards)]
public class SyncFuelCardTransactionsCommand : ICommand<Result<FuelCardSyncResultDto>>
{
    /// <summary>
    /// Sync only this provider; null syncs all active providers.
    /// </summary>
    public FuelCardProviderType? ProviderType { get; set; }
}
