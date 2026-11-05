using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

internal sealed class GetTripTimelineHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTripTimelineQuery, Result<TripTimelineDto>>
{
    public async Task<Result<TripTimelineDto>> Handle(
        GetTripTimelineQuery req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);

        if (trip is null)
        {
            return Result<TripTimelineDto>.Fail($"Could not find a trip with ID '{req.TripId}'");
        }

        var truckNumber = await TruckRefResolver.ResolveNumberAsync(tenantUow, trip.TruckId, ct);
        var events = BuildTimelineEvents(trip, truckNumber);
        var timeline = new TripTimelineDto
        {
            Events = events.OrderBy(e => e.Timestamp)
        };

        return Result<TripTimelineDto>.Ok(timeline);
    }

    private static List<TripTimelineEventDto> BuildTimelineEvents(Trip trip, string? truckNumber)
    {
        var events = new List<TripTimelineEventDto>
        {
            new()
            {
                Timestamp = trip.CreatedAt,
                EventType = "created",
                Description = $"Trip '{trip.Name}' was created"
            }
        };

        if (trip.DispatchedAt.HasValue)
        {
            events.Add(new TripTimelineEventDto
            {
                Timestamp = trip.DispatchedAt.Value,
                EventType = "dispatched",
                Description = truckNumber is not null
                    ? $"Trip dispatched to truck {truckNumber}"
                    : "Trip dispatched"
            });
        }

        // Add stop arrival events
        foreach (var stop in trip.Stops.Where(s => s.ArrivedAt.HasValue).OrderBy(s => s.ArrivedAt))
        {
            var stopType = stop.Type == TripStopType.PickUp ? "picked up" : "delivered";
            events.Add(new TripTimelineEventDto
            {
                Timestamp = stop.ArrivedAt!.Value,
                EventType = stop.Type == TripStopType.PickUp ? "pickup" : "delivery",
                Description = $"Load '{stop.Load.Name}' {stopType}",
                StopId = stop.Id,
                LoadId = stop.LoadId,
                LoadName = stop.Load.Name
            });
        }

        if (trip.CompletedAt.HasValue)
        {
            events.Add(new TripTimelineEventDto
            {
                Timestamp = trip.CompletedAt.Value,
                EventType = "completed",
                Description = "Trip completed successfully"
            });
        }

        if (trip.CancelledAt.HasValue)
        {
            events.Add(new TripTimelineEventDto
            {
                Timestamp = trip.CancelledAt.Value,
                EventType = "cancelled",
                Description = "Trip was cancelled"
            });
        }

        return events;
    }
}
