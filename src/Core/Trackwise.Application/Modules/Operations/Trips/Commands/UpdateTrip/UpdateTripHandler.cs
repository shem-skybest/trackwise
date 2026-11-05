using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Application.Modules.Operations.Loads.Services;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class UpdateTripHandler(
    ITenantUnitOfWork uow,
    ILoadService loadService,
    ITripCrewService crewService,
    ICrewPlanService crewPlan,
    ILogger<UpdateTripHandler> logger)
    : IAppRequestHandler<UpdateTripCommand, Result>
{
    public async Task<Result> Handle(UpdateTripCommand req, CancellationToken ct)
    {
        var trip = await uow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result.Fail($"Trip '{req.TripId}' not found.");
        }

        if (!TripStatusMachine.CanModify(trip.Status))
        {
            return Result.Fail("Only trips in 'Draft' or 'Dispatched' status can be updated.");
        }

        // Name field
        if (!string.IsNullOrEmpty(req.Name) && trip.Name != req.Name)
        {
            trip.Name = req.Name;
        }

        // Truck assignment/swap - use domain method which raises TripAssignedToTruckEvent
        Truck? newTruck = null;
        var truckChanged = false;
        if (req.TruckId is { } newTruckId && newTruckId != trip.TruckId)
        {
            newTruck = await uow.Repository<Truck>().GetByIdAsync(newTruckId, ct);
            if (newTruck is null)
            {
                return Result.Fail($"Truck '{newTruckId}' not found.");
            }

            try
            {
                trip.AssignToTruck(newTruck);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }

            truckChanged = true;
        }

        if (req.DriverId != trip.DriverId || req.HelperId != trip.HelperId || req.SecondHelperId != trip.SecondHelperId)
        {
            var crew = await crewService.ResolveAsync(req.DriverId, req.HelperId, req.SecondHelperId, ct);
            if (!crew.IsSuccess)
            {
                return Result.Fail(crew.Error!);
            }

            try
            {
                trip.AssignCrew(crew.Value!.Driver, crew.Value.Helper, crew.Value.SecondHelper);
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }

            if (trip.Status == TripStatus.Dispatched
                && await crewService.FindCrewConflictAsync(trip, ct) is { } conflict)
            {
                return Result.Fail(conflict);
            }

            var tripDay = crewPlan.TripDay(trip.GetLoads());
            if (await crewPlan.FindUnavailableAsync(tripDay, trip.GetCrewMemberIds(), ct) is { } unavailable)
            {
                return Result.Fail(unavailable);
            }
        }

        // A map of current loads on the trip for easy access, key is the load ID and value is the load entity
        var loadsMap = trip.GetLoads().ToDictionary(l => l.Id);

        // When truck changes, update all loads' truck assignment
        if (truckChanged)
        {
            foreach (var load in loadsMap.Values)
            {
                load.AssignedTruckId = trip.TruckId;
                load.AssignedTruck = newTruck;
            }

            logger.LogInformation(
                "Updated truck assignment for {LoadCount} loads in trip '{TripId}' to truck '{TruckId}'",
                loadsMap.Count, trip.Id, trip.TruckId);
        }

        int removedCount;
        try
        {
            removedCount = RemoveLoads(trip, loadsMap, req.DetachedLoadIds);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        var attachResult = await AttachExistingLoadsAsync(trip, loadsMap, req.AttachedLoadIds, ct);
        if (!attachResult.IsSuccess)
        {
            return Result.Fail(attachResult.Error!);
        }

        var attachedCount = attachResult.Value;
        var createdCount = await CreateNewLoadsAsync(trip, loadsMap, req.NewLoads);
        if (loadsMap.Count == 0)
        {
            return Result.Fail("A trip needs at least one drop - cancel the trip instead of removing them all.");
        }

        // Update stop order if optimized stops are provided
        if (req.OptimizedStops != null && req.OptimizedStops.Any())
        {
            UpdateStopOrder(trip, req.OptimizedStops);
        }

        // Always update total distance when provided (from route optimizer)
        if (req.TotalDistance.HasValue)
        {
            trip.TotalDistance = req.TotalDistance.Value;
        }

        // SaveChanges dispatches domain events (TripAssignedToTruckEvent if truck changed)
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Updated trip '{TripId}'. Name='{Name}', Truck='{TruckId}'. Loads={LoadCount} (attached {Attached}, created {Created}, removed {Removed})",
            trip.Id, trip.Name, trip.TruckId, loadsMap.Count, attachedCount, createdCount,
            removedCount);

        return Result.Ok();
    }

    private int RemoveLoads(Trip trip, Dictionary<Guid, Load> loadsMap, IEnumerable<Guid>? loadIdsToRemove)
    {
        if (loadIdsToRemove is null)
        {
            return 0;
        }

        var loadRepo = uow.Repository<Load>();

        var before = loadsMap.Count;
        foreach (var loadId in loadIdsToRemove.Distinct())
        {
            if (loadsMap.Remove(loadId, out var load))
            {
                trip.RemoveLoad(loadId);
                loadRepo.Delete(load);
            }
        }

        return before - loadsMap.Count;
    }

    private async Task<Result<int>> AttachExistingLoadsAsync(
        Trip trip,
        Dictionary<Guid, Load> loadsMap,
        IEnumerable<Guid>? attachIds,
        CancellationToken ct)
    {
        var count = 0;
        if (attachIds is null)
        {
            return Result<int>.Ok(count);
        }

        var ids = attachIds.Distinct().Where(id => !loadsMap.ContainsKey(id)).ToArray();
        if (ids.Length == 0)
        {
            return Result<int>.Ok(count);
        }

        var toAttach = await uow.Repository<Load>().GetListAsync(l => ids.Contains(l.Id), ct);

        foreach (var load in toAttach)
        {
            if (load.Status != LoadStatus.Draft)
            {
                return Result<int>.Fail(
                    $"Only loads in 'Draft' status can be attached. Load '{load.Id}' is in '{load.Status}' status.");
            }

            load.AssignedTruckId = trip.TruckId;
            load.AssignedTruck = trip.Truck;

            // If the load already has trip stops, we need to remove them first from the previous trip
            // to avoid duplicates. This can happen if the load was previously attached to another trip.
            if (load.TripStops.Count > 0)
            {
                var previousTrip = await uow.Repository<Trip>().GetByIdAsync(load.TripStops.First().TripId, ct);
                previousTrip?.RemoveLoad(load.Id);
            }

            // Add the existing load to the new trip
            trip.AddLoads([load]);

            // The trip is already tracked, so its new stops must be added explicitly - reached only
            // through the Stops collection, EF would save them as updates to rows that don't exist.
            foreach (var stop in trip.Stops.Where(s => s.LoadId == load.Id))
            {
                await uow.Repository<TripStop>().AddAsync(stop, ct);
            }

            loadsMap[load.Id] = load;
            count++;
        }

        return Result<int>.Ok(count);
    }

    private async Task<int> CreateNewLoadsAsync(
        Trip trip,
        Dictionary<Guid, Load> loadsMap,
        IEnumerable<CreateTripLoadCommand>? newLoadCommands)
    {
        if (newLoadCommands is null)
        {
            return 0;
        }

        var createdCount = 0;
        var loadParametersList = new List<CreateLoadParameters>();

        foreach (var c in newLoadCommands)
        {
            var createLoadParameters = new CreateLoadParameters(
                c.Name,
                c.Type,
                (c.OriginAddress, c.OriginLocation),
                (c.DestinationAddress, c.DestinationLocation),
                c.DeliveryCost,
                c.Distance,
                c.CustomerId,
                trip.TruckId,
                c.AssignedDispatcherId,
                trip.Id,
                RequestedPickupDate: c.RequestedPickupDate,
                RequestedDeliveryDate: c.RequestedDeliveryDate,
                Notes: c.Notes,
                OriginSiteId: c.OriginSiteId,
                DestinationSiteId: c.DestinationSiteId,
                BookingId: c.BookingId,
                Commodity: c.Commodity,
                PlannedBoxes: c.PlannedBoxes,
                PlannedBags: c.PlannedBags);

            loadParametersList.Add(createLoadParameters);
            createdCount++;
        }

        var newLoads = await loadService.CreateLoadsAsync(loadParametersList);
        foreach (var load in newLoads)
        {
            loadsMap[load.Id] = load;
        }

        return createdCount;
    }

    /// <summary>
    ///     Updates the order of existing trip stops based on the optimized stops from the route optimizer.
    /// </summary>
    private void UpdateStopOrder(Trip trip, IEnumerable<TripStopDto> optimizedStops)
    {
        // Build a lookup for the new order: key = (LoadId, Type), value = new Order
        var orderLookup = optimizedStops.ToDictionary(
            s => (s.LoadId, s.Type),
            s => s.Order);

        // Update the order of existing stops
        foreach (var stop in trip.Stops)
        {
            var key = (stop.LoadId, stop.Type);
            if (orderLookup.TryGetValue(key, out var newOrder))
            {
                stop.Order = newOrder;
            }
            else
            {
                logger.LogWarning(
                    "Could not find optimized order for stop with LoadId '{LoadId}' and Type '{Type}'",
                    stop.LoadId, stop.Type);
            }
        }
    }
}
