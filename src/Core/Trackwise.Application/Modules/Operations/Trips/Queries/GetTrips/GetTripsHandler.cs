using Trackwise.Application.Modules.IdentityAccess.Employees;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

internal sealed class GetTripsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTripsQuery, PagedResult<TripDto>>
{
    public async Task<PagedResult<TripDto>> Handle(
        GetTripsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Trip>().Query();

        if (!string.IsNullOrEmpty(req.Name))
        {
            query = query.Where(i => i.Name.Contains(req.Name));
        }

        if (req.Status.HasValue)
        {
            query = query.Where(i => i.Status == req.Status.Value);
        }

        if (!string.IsNullOrEmpty(req.TruckNumber))
        {
            query = query.Where(i => i.Truck != null && i.Truck.Number == req.TruckNumber);
        }

        if (req.TruckId.HasValue)
        {
            query = query.Where(i => i.TruckId == req.TruckId.Value);
        }

        if (req.DriverId.HasValue)
        {
            query = query.Where(i => i.DriverId == req.DriverId.Value);
        }

        if (req.StartDate.HasValue && req.EndDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= req.StartDate.Value && i.CreatedAt <= req.EndDate.Value);
        }

        if (req.OnlyActiveTrips)
        {
            query = query.Where(i => i.Status != TripStatus.Completed && i.Status != TripStatus.Cancelled);
        }

        var totalItems = query.Count();

        var trips = await query
            .OrderBy(req.OrderBy)
            .ApplyPaging(req.Page, req.PageSize)
            .ToArrayAsync(ct);

        var truckNumbers = await TruckRefResolver.ResolveAsync(
            tenantUow, trips.Select(t => t.TruckId).OfType<Guid>().Distinct().ToArray(), ct);

        var helperNames = await HelperRefResolver.ResolveAsync(
            tenantUow,
            trips.SelectMany(t => new[] { t.HelperId, t.SecondHelperId }).OfType<Guid>().Distinct().ToArray(),
            ct);

        var driverNames = await EmployeeRefResolver.ResolveAsync(
            tenantUow, trips.Select(t => t.DriverId).OfType<Guid>().Distinct().ToArray(), ct);

        var items = trips
            .Select(trip => trip.ToDto(new TripRefNames(
                trip.TruckId is { } truckId && truckNumbers.TryGetValue(truckId, out var truckRef)
                    ? truckRef.Number
                    : null,
                trip.DriverId is { } driverId && driverNames.TryGetValue(driverId, out var driverRef)
                    ? driverRef.Name
                    : null,
                NameOf(helperNames, trip.HelperId),
                NameOf(helperNames, trip.SecondHelperId))))
            .ToArray();

        return PagedResult<TripDto>.Ok(items, totalItems, req.PageSize);
    }

    private static string? NameOf(IReadOnlyDictionary<Guid, string> names, Guid? id) =>
        id is { } key && names.TryGetValue(key, out var name) ? name : null;
}
