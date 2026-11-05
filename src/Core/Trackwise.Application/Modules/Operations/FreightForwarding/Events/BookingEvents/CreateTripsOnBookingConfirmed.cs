using MediatR;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Events.BookingEvents;

// ── 3. Booking Confirmed -> Trips ────────────────────────────────────────────

public class CreateTripsOnBookingConfirmed
{
    //private readonly ITenantUnitOfWork _tenantUow;

    //public CreateTripsOnBookingConfirmed(ITenantUnitOfWork tenantUow) => _tenantUow = tenantUow;

    //public async Task Handle(BookingConfirmed e, CancellationToken ct)
    //{
    //    var bookingRepo = _tenantUow.Repository<Booking>();
    //    var tripRepo = _tenantUow.Repository<Trip>();

    //    if (await tripRepo.Query().AnyAsync(t => t.BookingId == e.Id, ct)) return;

    //    var booking = await bookingRepo.Query()
    //        .FirstOrDefaultAsync(b => b.Id == e.Id, ct)
    //        ?? throw new InvalidOperationException($"Booking {e.Id} not found.");

    //    // Trip uses booking.Lane (stops/windows), CargoSpec, TemperatureSpec, IsHazardous
    //    await tripRepo.AddAsync(Trip.CreateFromBooking(booking), ct);
    //}
}
