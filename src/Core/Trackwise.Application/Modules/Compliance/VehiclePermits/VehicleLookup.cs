using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits;

/// <summary>A truck or trailer as the papers screens and the reminder name it.</summary>
public sealed record VehicleRef(string Code, string Name, bool IsActive);

/// <summary>Resolves the vehicles behind a set of papers in two queries, instead of one navigation read per paper.</summary>
internal static class VehicleLookup
{
    public static async Task<Dictionary<Guid, VehicleRef>> LoadAsync(
        ITenantUnitOfWork tenantUow,
        IReadOnlyCollection<VehiclePermit> permits,
        CancellationToken ct)
    {
        var truckIds = permits.Where(p => p.TruckId.HasValue).Select(p => p.TruckId!.Value).Distinct().ToList();
        var trailerIds = permits.Where(p => p.TrailerId.HasValue).Select(p => p.TrailerId!.Value).Distinct().ToList();

        var trucks = truckIds.Count == 0
            ? []
            : await tenantUow.Repository<Truck>().Query()
                .Where(t => truckIds.Contains(t.Id))
                .Select(t => new { t.Id, Ref = new VehicleRef(t.Number, t.Name, t.IsActive) })
                .ToListAsync(ct);

        var trailers = trailerIds.Count == 0
            ? []
            : await tenantUow.Repository<Trailer>().Query()
                .Where(t => trailerIds.Contains(t.Id))
                .Select(t => new { t.Id, Ref = new VehicleRef(t.Code, t.Name, t.IsActive) })
                .ToListAsync(ct);

        return trucks.Concat(trailers).ToDictionary(v => v.Id, v => v.Ref);
    }

    public static VehicleRef? For(this Dictionary<Guid, VehicleRef> vehicles, VehiclePermit permit) =>
        vehicles.GetValueOrDefault(permit.TruckId ?? permit.TrailerId!.Value);
}
