using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Services;

/// <summary>
/// Pulls fuel card transactions from the current tenant's active providers, stages them
/// idempotently, auto-matches trucks, and materializes expenses. Shared by the manual
/// sync command and the nightly Hangfire job.
/// </summary>
public interface IFuelCardSyncService : IApplicationService
{
    Task<FuelCardSyncResultDto> SyncCurrentTenantAsync(
        FuelCardProviderType? providerType = null,
        CancellationToken ct = default);
}
