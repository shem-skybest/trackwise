using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetUnassignedBookingsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetUnassignedBookingsQuery, Result<List<UnAssignedBookingDto>>>
{
    // The planning board asks for a calendar DATE, but bookings store instants. This is the business
    // day's offset (Philippines, UTC+8). Replace with the tenant's/user's time zone if you have one.
    private static readonly TimeSpan BusinessUtcOffset = TimeSpan.FromHours(8);

    public async Task<Result<List<UnAssignedBookingDto>>> Handle(GetUnassignedBookingsQuery req, CancellationToken ct)
    {
        // Npgsql only accepts offset-0 DateTimeOffsets, so convert the day's bounds to UTC.
        var dayStart = new DateTimeOffset(req.Date.ToDateTime(TimeOnly.MinValue), BusinessUtcOffset).ToUniversalTime();
        var dayEnd = dayStart.AddDays(1);

        // Making the trip moves a booking to InProgress and cancelling the trip moves it back, so a
        // Confirmed booking is exactly one with no trip. Drafts are excluded: their documents or
        // credit check are still open, so they are not ready to plan.
        // RequestedPickupStart is computed from Lane, which is stored as JSON and is not mapped as a
        // scalar property. Keep the status predicate in SQL, then evaluate the computed date in memory.
        var bookings = (await tenantUow.Repository<Booking>().Query().AsNoTracking()
            .Where(b => b.Status == BookingStatus.Confirmed)
            .ToListAsync(ct))
            .Where(b => b.RequestedPickupStart >= dayStart
                && b.RequestedPickupStart < dayEnd)
            .OrderBy(b => b.RequestedPickupStart)
            .ThenBy(b => b.BookingNumber)
            .ToList();

        if (bookings.Count == 0)
            return Result<List<UnAssignedBookingDto>>.Ok([]);

        // One lookup each for every location on every lane, and for the customers.
        var locationIds = bookings
            .SelectMany(b => b.Lane.Stops.Select(s => s.LocationId))
            .Distinct()
            .ToList();
        var locationNames = await tenantUow.Repository<Location>().Query().AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .Select(l => new { l.Id, l.Name })
            .ToDictionaryAsync(l => l.Id, l => l.Name, ct);

        var customerIds = bookings.Select(b => b.CustomerId).Distinct().ToList();
        var customerNames = await tenantUow.Repository<Customer>().Query().AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var rows = bookings
            .Select(b => b.ToUnassignedDto(
                customerNames.GetValueOrDefault(b.CustomerId, "Unknown"),
                locationNames))
            .ToList();

        return Result<List<UnAssignedBookingDto>>.Ok(rows);
    }
}
