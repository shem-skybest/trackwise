using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Loads.Queries;

internal sealed class GetLoadsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetLoadsQuery, PagedResult<LoadDto>>
{
    public async Task<PagedResult<LoadDto>> Handle(
        GetLoadsQuery req,
        CancellationToken ct)
    {
        var baseQuery = tenantUow.Repository<Load>().Query();

        if (!string.IsNullOrEmpty(req.Search))
        {
            baseQuery = baseQuery.Where(i =>
                (i.Name != null && i.Name.Contains(req.Search)) ||
                (i.Customer != null && i.Customer.Name.Contains(req.Search)) ||
                i.Number.ToString().Contains(req.Search) ||
                i.OriginAddress.StreetAddress.Contains(req.Search) ||
                (i.OriginAddress.Subdivision != null && i.OriginAddress.Subdivision.Contains(req.Search)) ||
                i.DestinationAddress.StreetAddress.Contains(req.Search) ||
                (i.DestinationAddress.Subdivision != null && i.DestinationAddress.Subdivision.Contains(req.Search)));
        }

        if (req.OnlyActiveLoads)
        {
            baseQuery = baseQuery.Where(i =>
                i.Status == LoadStatus.Dispatched || i.Status == LoadStatus.PickedUp);
        }

        if (req.UserId.HasValue)
        {
            // Resolved separately, past the soft-delete filter, rather than filtering through the
            // AssignedTruck navigation: a join against Truck picks up its query filter too, which
            // would silently drop a driver's own delivered loads the moment their truck is retired.
            var driverTruckIds = await tenantUow.Repository<Truck>().Query()
                .IgnoreQueryFilters()
                .Where(t => t.MainDriverId == req.UserId || t.SecondaryDriverId == req.UserId)
                .Select(t => t.Id)
                .ToArrayAsync(ct);

            // Crews rotate trucks, so a driver's loads are also the ones on trips they drive or rescue
            var rescueIds = tenantUow.Repository<TripRescue>().Query()
                .Where(r => r.DriverId == req.UserId)
                .Select(r => (Guid?)r.Id);
            var tripLoadIds = tenantUow.Repository<TripStop>().Query()
                .Where(s => s.Trip.Status != TripStatus.Cancelled
                            && (s.Trip.DriverId == req.UserId || rescueIds.Contains(s.RescueId)))
                .Select(s => s.LoadId);

            baseQuery = baseQuery.Where(i =>
                (i.AssignedTruckId != null && driverTruckIds.Contains(i.AssignedTruckId.Value))
                || tripLoadIds.Contains(i.Id));
        }

        if (req.TruckId.HasValue)
        {
            baseQuery = baseQuery.Where(i => i.AssignedTruckId == req.TruckId);
        }

        if (req.Statuses?.Length > 0)
        {
            baseQuery = baseQuery.Where(i => req.Statuses.Contains(i.Status));
        }

        if (req.Types?.Length > 0)
        {
            baseQuery = baseQuery.Where(i => req.Types.Contains(i.Type));
        }

        if (req.CustomerId.HasValue)
        {
            baseQuery = baseQuery.Where(i => i.CustomerId == req.CustomerId.Value);
        }

        if (req is { StartDate: not null, EndDate: not null })
        {
            baseQuery = baseQuery.Where(i => i.DispatchedAt >= req.StartDate &&
                                             i.DispatchedAt <= req.EndDate);
        }

        var totalItems = await baseQuery.CountAsync(ct);

        baseQuery = baseQuery.OrderBy(req.OrderBy);

        if (!req.LoadAllPages)
        {
            baseQuery = baseQuery.ApplyPaging(req.Page, req.PageSize);
        }

        var entities = await baseQuery.ToArrayAsync(ct);
        var intermodal = await LoadIntermodalResolver.ResolveAsync(tenantUow, entities, ct);
        var loads = entities.Select(i => i.ToDto(intermodal)).ToArray();

        return PagedResult<LoadDto>.Ok(loads, totalItems, req.PageSize);
    }
}
