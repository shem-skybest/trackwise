using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CompleteBookingHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CompleteBookingCommand, Result>
{
    public async Task<Result> Handle(CompleteBookingCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().GetByIdAsync(req.Id, ct);
        if (booking is null)
        {
            return Result.Fail($"Booking with ID '{req.Id}' not found.");
        }

        if (booking.Status != BookingStatus.InProgress)
        {
            return Result.Fail("Only bookings InExecution can be completed.");
        }

        booking.Complete();

        tenantUow.Repository<Booking>().Update(booking);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
