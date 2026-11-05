using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordTripReportHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    ILogger<RecordTripReportHandler> logger)
    : IAppRequestHandler<RecordTripReportCommand, Result>
{
    public async Task<Result> Handle(RecordTripReportCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        var stops = trip.Stops.ToDictionary(s => s.Id);
        if (req.Stops.FirstOrDefault(e => !stops.ContainsKey(e.StopId)) is { } unknown)
        {
            return Result.Fail($"Stop '{unknown.StopId}' isn't on this trip.");
        }

        var dropNumbers = trip.Stops
            .Where(s => s.Type == TripStopType.DropOff)
            .OrderBy(s => s.Order)
            .Select((s, i) => (s.Id, Number: i + 1))
            .ToDictionary(p => p.Id, p => p.Number);
        var recordedBy = currentUser.GetUserName();

        foreach (var entry in req.Stops.OrderBy(e => stops[e.StopId].Order))
        {
            try
            {
                Apply(trip, entry, recordedBy);
            }
            catch (InvalidOperationException ex)
            {
                var stop = stops[entry.StopId];
                var label = stop.Type == TripStopType.PickUp ? "Warehouse" : $"Drop {dropNumbers[stop.Id]}";
                return Result.Fail($"{label}: {ex.Message}");
            }
        }

        // Last, so a draft trip has been moved on by its first pickup and the garage times are
        // checked against every stop time the report just added.
        if (req.GarageDepartedAt.HasValue || req.GarageArrivedAt.HasValue)
        {
            try
            {
                trip.RecordGarageTimes(req.GarageDepartedAt?.ToUniversalTime(), req.GarageArrivedAt?.ToUniversalTime());
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail($"Garage: {ex.Message}");
            }
        }

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Recorded a report of {StopCount} stop(s) for trip '{TripName}' (ID: {TripId})",
            req.Stops.Count, trip.Name, trip.Id);
        return Result.Ok();
    }

    /// <summary>
    /// Arrival then departure, each only where not recorded yet. A pickup's entry applies to the
    /// whole warehouse visit.
    /// </summary>
    private static void Apply(Trip trip, TripReportStopEntry entry, string? recordedBy)
    {
        var visit = trip.GetSameVisitPickups(entry.StopId);

        if (entry.ArrivedAt is { } arrivedAt)
        {
            foreach (var stop in visit.Where(s => !s.ArrivedAt.HasValue))
            {
                trip.MarkStopArrived(stop.Id, entry.TemperatureAtArrival, arrivedAt.ToUniversalTime(), recordedBy);
            }
        }

        if (entry.DepartedAt is not { } departedAt)
        {
            return;
        }

        var departure = new StopDeparture(
            entry.Outcome,
            entry.TemperatureAtDeparture,
            departedAt.ToUniversalTime(),
            entry.HandlingStartedAt?.ToUniversalTime(),
            entry.HandlingFinishedAt?.ToUniversalTime(),
            entry.ReturnedBoxes,
            entry.ReturnedBags,
            entry.ReturnReason,
            string.IsNullOrWhiteSpace(entry.OutcomeNote) ? null : entry.OutcomeNote.Trim());

        foreach (var stop in visit.Where(s => !s.DepartedAt.HasValue))
        {
            trip.MarkStopDeparted(stop.Id, departure, recordedBy);
        }
    }
}
