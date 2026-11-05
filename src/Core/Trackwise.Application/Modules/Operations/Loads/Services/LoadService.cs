using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.Application.Modules.Operations.Loads.Services;

internal sealed class LoadService(ITenantUnitOfWork tenantUow) : ILoadService
{
    public async Task<Load> CreateLoadAsync(CreateLoadParameters parameters, bool saveChanges = true,
        CancellationToken ct = default)
    {
        var created = await CreateLoadsAsync([parameters], saveChanges, ct);
        return created.First();
    }

    public async Task<IReadOnlyCollection<Load>> CreateLoadsAsync(IEnumerable<CreateLoadParameters> parameters,
        bool saveChanges = true, CancellationToken ct = default)
    {
        var paramList = parameters as IList<CreateLoadParameters> ?? parameters.ToList();
        if (paramList.Count == 0)
        {
            return [];
        }

        // 1) Collect distinct foreign keys (filter out null truck IDs)
        var dispatcherIds = paramList.Select(p => p.DispatcherId).Distinct().ToList();
        var truckIds = paramList.Where(p => p.TruckId.HasValue).Select(p => p.TruckId!.Value).Distinct().ToList();
        var customerIds = paramList.Select(p => p.CustomerId).Distinct().ToList();
        var tripIds = paramList.Select(p => p.TripId).Distinct().ToList();
        var bookingIds = paramList.Where(p => p.BookingId.HasValue).Select(p => p.BookingId!.Value).Distinct().ToList();
        var siteIds = paramList.SelectMany(p => new[] { p.OriginSiteId, p.DestinationSiteId })
            .OfType<Guid>().Distinct().ToList();

        // 2) Batch-load referenced rows (at most 4 queries total)
        var dispatcherRepo = tenantUow.Repository<Employee>();
        var truckRepo = tenantUow.Repository<Truck>();
        var customerRepo = tenantUow.Repository<Customer>();
        var tripRepo = tenantUow.Repository<Trip>();
        var bookingRepo = tenantUow.Repository<Booking>();

        // Note: EF Core DbContext does not allow parallel queries on the same context;
        // keep these sequential to avoid InvalidOperationException.
        var dispatchers = await dispatcherRepo.GetListAsync(e => dispatcherIds.Contains(e.Id), ct);
        var trucks = await truckRepo.GetListAsync(t => truckIds.Contains(t.Id), ct);
        var customers = await customerRepo.GetListAsync(c => customerIds.Contains(c.Id), ct);
        var trips = await tripRepo.GetListAsync(t => tripIds.Contains(t.Id), ct);
        var bookings = bookingIds.Count > 0
            ? await bookingRepo.GetListAsync(b => bookingIds.Contains(b.Id), ct)
            : [];
        var sites = siteIds.Count > 0
            ? await tenantUow.Repository<Location>().GetListAsync(l => siteIds.Contains(l.Id), ct)
            : [];

        var dispatcherById = dispatchers.ToDictionary(e => e.Id);
        var truckById = trucks.ToDictionary(t => t.Id);
        var customerById = customers.ToDictionary(c => c.Id);
        var tripById = trips.ToDictionary(t => t.Id);
        var bookingById = bookings.ToDictionary(b => b.Id);
        var siteById = sites.ToDictionary(l => l.Id);

        // 3) Validate missing references (fail fast with precise info)
        var missingDispatchers = dispatcherIds.Where(id => !dispatcherById.ContainsKey(id)).ToList();
        var missingTrucks = truckIds.Where(id => !truckById.ContainsKey(id)).ToList();
        var missingCustomers = customerIds.Where(id => !customerById.ContainsKey(id)).ToList();
        var missingBookings = bookingIds.Where(id => !bookingById.ContainsKey(id)).ToList();
        var missingSites = siteIds.Where(id => !siteById.ContainsKey(id)).ToList();
        var siteRefusals = paramList
            .SelectMany(p => new[] { p.OriginSiteId, p.DestinationSiteId }
                .OfType<Guid>()
                .Where(siteById.ContainsKey)
                .Select(id => LoadSiteRules.Refusal(siteById[id], p.CustomerId, isNewLink: true)))
            .OfType<string>()
            .Distinct()
            .ToList();
        var invalidBookings = bookingIds
            .Where(id => bookingById.TryGetValue(id, out var b)
                && b.Status != BookingStatus.Confirmed && b.Status != BookingStatus.InProgress)
            .ToList();

        if (missingDispatchers.Count > 0 || missingTrucks.Count > 0 || missingCustomers.Count > 0
            || missingBookings.Count > 0 || invalidBookings.Count > 0
            || missingSites.Count > 0 || siteRefusals.Count > 0)
        {
            // Aggregate into a single, developer-friendly error message
            var problems = new List<string>(5);
            if (missingDispatchers.Count > 0)
            {
                problems.Add($"Dispatchers missing: {string.Join(", ", missingDispatchers)}");
            }

            if (missingTrucks.Count > 0)
            {
                problems.Add($"Trucks missing: {string.Join(", ", missingTrucks)}");
            }

            if (missingCustomers.Count > 0)
            {
                problems.Add($"Customers missing: {string.Join(", ", missingCustomers)}");
            }

            if (missingBookings.Count > 0)
            {
                problems.Add($"Bookings missing: {string.Join(", ", missingBookings)}");
            }

            if (invalidBookings.Count > 0)
            {
                problems.Add($"Bookings must be Confirmed or InExecution to attach a Load: {string.Join(", ", invalidBookings)}");
            }

            if (missingSites.Count > 0)
            {
                problems.Add($"Sites missing: {string.Join(", ", missingSites)}");
            }

            problems.AddRange(siteRefusals);

            throw new InvalidOperationException(string.Join(" | ", problems));
        }

        // 4) Create domain entities in memory
        var loads = new List<Load>(paramList.Count);
        foreach (var p in paramList)
        {
            var dispatcher = dispatcherById[p.DispatcherId];
            var truck = p.TruckId.HasValue ? truckById[p.TruckId.Value] : null;
            var customer = customerById[p.CustomerId];
            Trip? trip = null;

            if (p.TripId.HasValue)
            {
                trip = tripById[p.TripId.Value];
            }

            var load = Load.Create(
                p.Name,
                p.Type,
                p.DeliveryCost,
                p.Origin.address,
                p.Origin.location,
                p.Destination.address,
                p.Destination.location,
                customer,
                truck,
                dispatcher,
                trip
            );

            load.Distance = p.Distance;
            load.Source = p.Source;
            load.RequestedPickupDate = p.RequestedPickupDate;
            load.RequestedDeliveryDate = p.RequestedDeliveryDate;
            load.Notes = p.Notes;
            load.Commodity = p.Commodity;
            load.PlannedBoxes = p.PlannedBoxes;
            load.PlannedBags = p.PlannedBags;
            load.ContainerId = p.ContainerId;
            load.OriginTerminalId = p.OriginTerminalId;
            load.DestinationTerminalId = p.DestinationTerminalId;
            load.OriginSiteId = p.OriginSiteId;
            load.DestinationSiteId = p.DestinationSiteId;
            load.BookingId = p.BookingId;
            load.IsHazmat = p.IsHazmat;
            load.HazmatClass = p.HazmatClass;
            load.UnNumber = p.UnNumber;
            loads.Add(load);
        }

        // 5) Single batched add + single SaveChanges
        await tenantUow.Repository<Load>().AddRangeAsync(loads, ct);

        if (saveChanges)
        {
            await tenantUow.SaveChangesAsync(ct);
        }

        return loads;
    }

    public async Task DeleteLoadAsync(Guid loadId)
    {
        var load = await tenantUow.Repository<Load>().GetByIdAsync(loadId);
        tenantUow.Repository<Load>().Delete(load);
        await tenantUow.SaveChangesAsync();
    }
}
