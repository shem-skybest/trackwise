using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.ConfirmBooking;

internal sealed class ConfirmBookingHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ConfirmBookingCommand, Result>
{
    public async Task<Result> Handle(ConfirmBookingCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().Query()
        .Include(b => b.Charges)
        .Include(b => b.Documents)
        .FirstOrDefaultAsync(b => b.Id == req.Id, ct);

        if (booking is null)
        {
            return Result.Fail($"Booking with ID '{req.Id}' not found.");
        }

        if (booking.Status != BookingStatus.Draft)
        {
            return Result.Fail("Only Pending bookings can be confirmed.");
        }

        booking.Confirm(req.ConfirmedBy);

        tenantUow.Repository<Booking>().Update(booking);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
