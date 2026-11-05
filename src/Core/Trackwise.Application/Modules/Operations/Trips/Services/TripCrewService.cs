using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Services;

internal sealed class TripCrewService(ITenantUnitOfWork tenantUow) : ITripCrewService
{
    public async Task<Result<TripCrew>> ResolveAsync(
        Guid? driverId, Guid? helperId, Guid? secondHelperId, CancellationToken ct = default)
    {
        Employee? driver = null;
        if (driverId is { } id)
        {
            driver = await tenantUow.Repository<Employee>().GetByIdAsync(id, ct);
            if (driver is null)
            {
                return Result<TripCrew>.Fail($"Could not find the driver with ID '{id}'");
            }
        }

        var helper = await FindHelperAsync(helperId, ct);
        if (helperId.HasValue && helper is null)
        {
            return Result<TripCrew>.Fail($"Could not find the helper with ID '{helperId}'");
        }

        var secondHelper = await FindHelperAsync(secondHelperId, ct);
        if (secondHelperId.HasValue && secondHelper is null)
        {
            return Result<TripCrew>.Fail($"Could not find the helper with ID '{secondHelperId}'");
        }

        return Result<TripCrew>.Ok(new TripCrew(driver, helper, secondHelper));
    }

    public async Task<string?> FindCrewConflictAsync(Trip trip, CancellationToken ct = default)
    {
        var (driverId, helperIds) = trip.GetCrewIds();
        if (driverId is null && helperIds.Count == 0)
        {
            return null;
        }

        var tripId = trip.Id;
        var truckId = trip.TruckId;
        var clash = await tenantUow.Repository<Trip>().Query()
            .Where(t => t.Id != tripId)
            .Where(t => t.Status == TripStatus.Dispatched || t.Status == TripStatus.InTransit)
            .Where(t => t.TruckId != truckId)
            .Where(t => (driverId != null && t.DriverId == driverId)
                        || (t.HelperId != null && helperIds.Contains(t.HelperId.Value))
                        || (t.SecondHelperId != null && helperIds.Contains(t.SecondHelperId.Value)))
            .Select(t => new
            {
                t.Number,
                t.Name,
                t.Status,
                Truck = t.Truck != null ? t.Truck.Number : null,
                t.DriverId,
                t.HelperId,
                t.SecondHelperId
            })
            .FirstOrDefaultAsync(ct);
        if (clash is null)
        {
            return null;
        }

        var who = driverId is { } id && clash.DriverId == id
            ? trip.Driver?.GetFullName()
            : new[] { trip.Helper, trip.SecondHelper }
                .FirstOrDefault(h => h is not null && (h.Id == clash.HelperId || h.Id == clash.SecondHelperId))?.Name;

        return $"{who ?? "Someone on this trip's crew"} is already on trip #{clash.Number} ('{clash.Name}') " +
               $"on {clash.Truck ?? "another truck"}, which is still {clash.Status.GetDescription().ToLowerInvariant()}. " +
               "Complete or cancel that trip first, or change this trip's crew.";
    }

    private async Task<Helper?> FindHelperAsync(Guid? helperId, CancellationToken ct) =>
        helperId is { } id ? await tenantUow.Repository<Helper>().GetByIdAsync(id, ct) : null;
}
