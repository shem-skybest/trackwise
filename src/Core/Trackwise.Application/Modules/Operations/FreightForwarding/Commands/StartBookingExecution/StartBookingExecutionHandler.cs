using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class StartBookingExecutionHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<StartBookingExecutionCommand, Result>
{
    public async Task<Result> Handle(StartBookingExecutionCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().GetByIdAsync(req.Id, ct);
        if (booking is null)
        {
            return Result.Fail($"Booking with ID '{req.Id}' not found.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            return Result.Fail("Only Confirmed bookings can start execution.");
        }

        //booking.StartExecution();

        tenantUow.Repository<Booking>().Update(booking);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
