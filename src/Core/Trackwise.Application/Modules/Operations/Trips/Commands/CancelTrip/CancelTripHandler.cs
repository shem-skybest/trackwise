using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Logging;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class CancelTripHandler(
    ITenantUnitOfWork tenantUow,
    ILogger<CancelTripHandler> logger)
    : IAppRequestHandler<CancelTripCommand, Result>
{
    public async Task<Result> Handle(CancelTripCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);

        if (trip is null)
        {
            return Result.Fail($"Could not find the trip with ID '{req.TripId}'");
        }

        try
        {
            trip.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await ReopenColdChainBookingsAsync(trip, ct);
        await tenantUow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Cancelled trip '{TripName}' with ID '{TripId}'. Reason: {Reason}",
            trip.Name, trip.Id, req.Reason ?? "Not specified");

        return Result.Ok();
    }

    /// <summary>
    /// The trip's loads are cancelled with it, so a Cold Chain booking it was running goes back to
    /// Confirmed and waits for a new trip.
    /// </summary>
    private async Task ReopenColdChainBookingsAsync(Trip trip, CancellationToken ct)
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

        foreach (var booking in bookings)
        {
            booking.ReturnToConfirmed();
        }
    }
}
