using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Events;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Routing;

namespace Trackwise.Application.Modules.Operations.Trips.Events;

internal sealed class TripDispatchedHandler(
    ILogger<TripDispatchedHandler> logger,
    ITenantUnitOfWork tenantUow,
    ITripTrackingService tripTrackingService)
    : IDomainEventHandler<TripDispatchedEvent>
{
    public async Task Handle(TripDispatchedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Trip dispatched: {TripId}", @event.TripId);

        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(@event.TripId, cancellationToken);
        if (trip is null)
        {
            logger.LogWarning("Trip not found for dispatched event: {TripId}", @event.TripId);
            return;
        }

        await RecordGarageDepartureAsync(trip, cancellationToken);

        var tenantId = tenantUow.GetCurrentTenant().Id;

        // Broadcast trip status update
        await tripTrackingService.BroadcastTripStatusUpdateAsync(
            tenantId,
            new TripStatusUpdateDto
            {
                TripId = @event.TripId,
                TripName = trip.Name,
                Status = TripStatus.Dispatched,
                PreviousStatus = TripStatus.Draft,
                UpdatedAt = DateTime.UtcNow
            });

        // Broadcast dispatch board update
        await tripTrackingService.BroadcastDispatchBoardUpdateAsync(
            tenantId,
            new DispatchBoardUpdateDto
            {
                UpdateType = DispatchBoardUpdateType.TripDispatched,
                EntityId = @event.TripId,
                UpdatedAt = DateTime.UtcNow
            });
    }

    /// <summary>
    /// Garage departure lives on the Workfile (one per Booking), not the Trip - a job's day of
    /// execution is one commercial file even if it turns into several drop-off Loads. Populated
    /// here rather than entered independently by ops, so it can't drift from the one real event.
    /// A trip with no linked booking, or one whose workfile type doesn't track garage movements,
    /// is a no-op - most trips have neither.
    /// </summary>
    private async Task RecordGarageDepartureAsync(Trip trip, CancellationToken ct)
    {
        var bookingIds = trip.GetLoads().Select(l => l.BookingId).OfType<Guid>().Distinct().ToArray();
        if (bookingIds.Length != 1)
        {
            return;
        }

        var workfile = await tenantUow.Repository<Workfile>().Query()
            .FirstOrDefaultAsync(w => w.BookingId == bookingIds[0], ct);

        if (workfile is null || !workfile.SupportsGarageTracking)
        {
            return;
        }

        var at = trip.DispatchedAt is { } dispatchedAt
            ? new DateTimeOffset(dispatchedAt, TimeSpan.Zero)
            : DateTimeOffset.UtcNow;

        workfile.RecordGarageDeparture(at);
        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
    }
}
