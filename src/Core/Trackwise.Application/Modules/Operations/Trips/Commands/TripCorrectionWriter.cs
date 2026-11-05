using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// Registers a correction's rows. They reach the tracker through no navigation, so each must be
/// added explicitly - otherwise the save silently skips them.
/// </summary>
internal static class TripCorrectionWriter
{
    public static async Task AddAsync(
        ITenantUnitOfWork tenantUow, IEnumerable<TripCorrection> changes, CancellationToken ct)
    {
        var repository = tenantUow.Repository<TripCorrection>();
        foreach (var change in changes)
        {
            await repository.AddAsync(change, ct);
        }
    }
}
