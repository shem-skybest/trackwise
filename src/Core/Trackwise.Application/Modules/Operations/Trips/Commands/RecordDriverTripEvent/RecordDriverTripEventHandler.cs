using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.Operations.Loads;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class RecordDriverTripEventHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUser,
    ILogger<RecordDriverTripEventHandler> logger)
    : IAppRequestHandler<RecordDriverTripEventCommand, Result>
{
    private const string AlreadyRecorded = "Already recorded - check the trip for the time on record.";

    public async Task<Result> Handle(RecordDriverTripEventCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        var access = await DriverTripAccess.CheckAsync(tenantUow, currentUser.GetUserId(), trip, req.StopId, ct);
        if (!access.IsSuccess)
        {
            return access;
        }

        var at = req.At?.ToUniversalTime();
        try
        {
            if (!Apply(trip, req, at))
            {
                return Result.Fail(AlreadyRecorded, ErrorCodes.TripEventAlreadyRecorded);
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Driver app recorded {Kind} on trip '{TripName}' (ID: {TripId}), stop {StopId}",
            req.Kind, trip.Name, trip.Id, req.StopId);
        return Result.Ok();
    }

    /// <summary>Applies the event to every stop of the visit still missing it; false when none was.</summary>
    private bool Apply(Trip trip, RecordDriverTripEventCommand req, DateTime? at)
    {
        const TripEventSource app = TripEventSource.DriverApp;

        // A tap the phone resends after losing signal may have landed the first time, or the office
        // entered it meanwhile; either way it's on record, which is what the crew wanted.
        switch (req.Kind)
        {
            case TripEventKind.GarageDeparted:
                if (trip.GarageDepartedAt.HasValue) return false;
                trip.MarkGarageDeparted(at, app);
                return true;
            case TripEventKind.GarageArrived:
                if (trip.GarageArrivedAt.HasValue) return false;
                trip.MarkGarageArrived(at, app);
                return true;
        }

        var visit = trip.GetSameVisitPickups(req.StopId!.Value);
        var pending = req.Kind switch
        {
            TripEventKind.Arrived => visit.Where(s => !s.ArrivedAt.HasValue),
            TripEventKind.HandlingStarted => visit.Where(s => !s.HandlingStartedAt.HasValue && !s.DepartedAt.HasValue),
            TripEventKind.HandlingFinished => visit.Where(s => !s.HandlingFinishedAt.HasValue && !s.DepartedAt.HasValue),
            TripEventKind.Departed => visit.Where(s => !s.DepartedAt.HasValue),
            _ => throw new ArgumentOutOfRangeException(nameof(req), req.Kind, null)
        };

        var stops = pending.ToList();
        var recordedBy = currentUser.GetUserName();
        foreach (var stop in stops)
        {
            switch (req.Kind)
            {
                case TripEventKind.Arrived:
                    trip.MarkStopArrived(stop.Id, req.Temperature, at, recordedBy, app);
                    break;
                case TripEventKind.HandlingStarted:
                    trip.MarkHandlingStarted(stop.Id, at, app);
                    break;
                case TripEventKind.HandlingFinished:
                    trip.MarkHandlingFinished(stop.Id, at, app);
                    break;
                case TripEventKind.Departed:
                    var isDrop = stop.Type == TripStopType.DropOff;
                    trip.MarkStopDeparted(
                        stop.Id,
                        new StopDeparture(
                            isDrop ? req.Outcome : null,
                            req.Temperature,
                            at,
                            ReturnedBoxes: isDrop ? req.ReturnedBoxes : null,
                            ReturnedBags: isDrop ? req.ReturnedBags : null,
                            ReturnReason: isDrop ? req.ReturnReason : null,
                            OutcomeNote: isDrop && !string.IsNullOrWhiteSpace(req.OutcomeNote) ? req.OutcomeNote.Trim() : null),
                        recordedBy,
                        app);
                    break;
            }
        }

        return stops.Count > 0;
    }
}
