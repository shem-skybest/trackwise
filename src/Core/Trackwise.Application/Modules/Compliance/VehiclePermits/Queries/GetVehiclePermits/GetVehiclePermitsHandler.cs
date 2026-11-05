using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Queries;

internal sealed class GetVehiclePermitsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetVehiclePermitsQuery, Result<IList<VehiclePermitDto>>>
{
    public async Task<Result<IList<VehiclePermitDto>>> Handle(GetVehiclePermitsQuery req, CancellationToken ct)
    {
        var singleVehicle = req.TruckId.HasValue || req.TrailerId.HasValue;
        var query = tenantUow.Repository<VehiclePermit>().Query();
        if (req.TruckId is { } truckId)
        {
            query = query.Where(p => p.TruckId == truckId);
        }

        if (req.TrailerId is { } trailerId)
        {
            query = query.Where(p => p.TrailerId == trailerId);
        }

        // "Current" is decided per vehicle and type, so it needs every paper of that type - filter
        // by type before, and by expiry only after, working out which ones are current.
        if (req.Type is { } type)
        {
            query = query.Where(p => p.Type == type);
        }

        var permits = await query.ToListAsync(ct);
        var current = VehiclePermitRules.CurrentIds(permits);
        var vehicles = await VehicleLookup.LoadAsync(tenantUow, permits, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        IEnumerable<VehiclePermit> visible = permits;
        if (!singleVehicle)
        {
            visible = visible.Where(p => current.Contains(p.Id) && vehicles.For(p) is { IsActive: true });
        }

        if (req.ExpiringWithinDays is { } days)
        {
            visible = visible.Where(p => current.Contains(p.Id) && p.DaysUntilExpiry(today) <= days);
        }

        var dtos = visible
            .OrderBy(p => p.ExpiresOn)
            .Select(p => p.ToDto(today, current.Contains(p.Id), vehicles.For(p) is { } v ? (v.Code, v.Name) : null))
            .ToList();

        return Result<IList<VehiclePermitDto>>.Ok(dtos);
    }
}
