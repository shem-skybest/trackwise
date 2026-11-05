using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Claims;
using Trackwise.Shared.Identity.Policies;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Loads;

internal static class DriverLoadAccess
{
    public const string NotYourLoad = "This load isn't on your trip.";

    /// <summary>
    /// Every driver holds the permissions behind the driver endpoints, which alone would let one
    /// confirm, geofence or photograph a colleague's load. A load belongs to the driver of a live trip
    /// carrying it, the driver of a rescue holding one of its stops, or - on no trip at all - its
    /// truck's main driver. <c>Load.Manage</c> (the office) acts on any load.
    /// </summary>
    public static async Task<bool> CanActOnAsync(
        ITenantUnitOfWork tenantUow,
        Guid? callerId,
        Guid loadId,
        CancellationToken ct)
    {
        if (callerId is not { } caller)
        {
            return false;
        }

        var stops = tenantUow.Repository<TripStop>()
            .Query()
            .Where(s => s.LoadId == loadId && s.Trip.Status != TripStatus.Cancelled);

        if (await stops.AnyAsync(s => s.Trip.DriverId == caller, ct))
        {
            return true;
        }

        var rescueIds = stops.Where(s => s.RescueId != null).Select(s => s.RescueId);
        if (await tenantUow.Repository<TripRescue>()
                .Query()
                .AnyAsync(r => rescueIds.Contains(r.Id) && r.DriverId == caller, ct))
        {
            return true;
        }

        if (!await stops.AnyAsync(ct)
            && await tenantUow.Repository<Load>()
                .Query()
                .AnyAsync(l => l.Id == loadId && l.AssignedTruck!.MainDriverId == caller, ct))
        {
            return true;
        }

        // The tenant's own claims, not TenantRolePermissions - roles are editable per tenant.
        return await tenantUow.Repository<Employee>()
            .Query()
            .Where(e => e.Id == caller)
            .SelectMany(e => e.Role!.Claims)
            .AnyAsync(
                c => c.ClaimType == CustomClaimTypes.Permission
                     && c.ClaimValue == Permission.Load.Manage,
                ct);
    }
}
