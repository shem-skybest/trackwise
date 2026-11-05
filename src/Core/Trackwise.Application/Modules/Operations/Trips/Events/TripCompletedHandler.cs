using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Events;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions.Routing;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.Application.Modules.Operations.Trips.Events;

internal sealed class TripCompletedHandler(
    ILogger<TripCompletedHandler> logger,
    ITenantUnitOfWork tenantUow,
    ITripTrackingService tripTrackingService)
    : IDomainEventHandler<TripCompletedEvent>
{
    public async Task Handle(TripCompletedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation("Trip completed: {TripId}", @event.TripId);

        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(@event.TripId, cancellationToken);
        if (trip is null)
        {
            logger.LogWarning("Trip not found for completed event: {TripId}", @event.TripId);
            return;
        }

        await RecordGarageArrivalAsync(trip, cancellationToken);
        await CompleteColdChainBookingsAsync(trip, cancellationToken);

        var tenantId = tenantUow.GetCurrentTenant().Id;

        // Broadcast trip status update
        await tripTrackingService.BroadcastTripStatusUpdateAsync(
            tenantId,
            new TripStatusUpdateDto
            {
                TripId = @event.TripId,
                TripName = trip.Name,
                Status = TripStatus.Completed,
                PreviousStatus = TripStatus.InTransit,
                UpdatedAt = DateTime.UtcNow
            });
    }

    /// <summary>
    /// A Cold Chain booking is one truck-trip, so the trip finishing finishes the booking. Forwarding
    /// bookings span more than one trip and are completed by hand.
    /// </summary>
    private async Task CompleteColdChainBookingsAsync(Trip trip, CancellationToken ct)
    {
        var bookingIds = trip.GetLoads().Select(l => l.BookingId).OfType<Guid>().Distinct().ToList();
        if (bookingIds.Count == 0)
        {
            return;
        }

        var bookings = await tenantUow.Repository<Booking>().Query()
            .Where(b => bookingIds.Contains(b.Id)
                && b.RateAgreementId != null
                && b.Status == BookingStatus.InProgress)
            .ToListAsync(ct);

        if (bookings.Count == 0)
        {
            return;
        }

        foreach (var booking in bookings)
        {
            booking.Complete();
        }

        await tenantUow.SaveChangesAsync(ct);
    }

    /// <summary>See <c>TripDispatchedHandler.RecordGarageDepartureAsync</c> for why this lives here
    /// rather than being entered independently by ops.</summary>
    private async Task RecordGarageArrivalAsync(Trip trip, CancellationToken ct)
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

        var at = trip.CompletedAt is { } completedAt
            ? new DateTimeOffset(completedAt, TimeSpan.Zero)
            : DateTimeOffset.UtcNow;

        workfile.RecordGarageArrival(at);
        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
    }
}
