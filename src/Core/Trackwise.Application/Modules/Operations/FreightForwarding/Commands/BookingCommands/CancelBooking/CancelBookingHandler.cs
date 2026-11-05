using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.CancelBooking;

internal sealed class CancelBookingHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CancelBookingCommand, Result>
{
    public async Task<Result> Handle(CancelBookingCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().GetByIdAsync(req.Id, ct);
        if (booking is null)
        {
            return Result.Fail($"Booking with ID '{req.Id}' not found.");
        }

        if (booking.Status != BookingStatus.Draft && booking.Status != BookingStatus.Confirmed)
        {
            return Result.Fail("Only Pending or Confirmed bookings can be cancelled.");
        }

        var reason = string.IsNullOrWhiteSpace(req.Reason)
            ? req.LossReason!.Value.GetDescription()
            : req.Reason.Trim();

        try
        {
            booking.Cancel(req.CancelledBy, reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        tenantUow.Repository<Booking>().Update(booking);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
