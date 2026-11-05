using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetBookingOperationalStatusHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SetBookingOperationalStatusCommand, Result>
{
    public async Task<Result> Handle(SetBookingOperationalStatusCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().GetByIdAsync(req.Id, ct);
        if (booking is null)
        {
            return Result.Fail($"Booking with ID '{req.Id}' not found.");
        }

        if (booking.Status != BookingStatus.Draft && booking.Status != BookingStatus.Confirmed
            && booking.Status != BookingStatus.Hold)
        {
            return Result.Fail("Only a Pending, Confirmed, or Hold booking can change to this status.");
        }

        try
        {
            switch (req.Status)
            {
                //case BookingStatus.Resched:
                //    booking.Reschedule(req.Note);
                //    break;
                //case BookingStatus.Hold:
                //    booking.Hold(req.Note);
                //    break;
                //case BookingStatus.LostOpportunity:
                //    booking.MarkLostOpportunity(req.Note, req.LossReason);
                //    break;
                //case BookingStatus.NoBooking:
                //    booking.MarkNoBooking(req.Note);
                //    break;
                default:
                    return Result.Fail(
                        $"'{req.Status}' is not one of Resched, Hold, LostOpportunity, NoBooking.");
            }
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
