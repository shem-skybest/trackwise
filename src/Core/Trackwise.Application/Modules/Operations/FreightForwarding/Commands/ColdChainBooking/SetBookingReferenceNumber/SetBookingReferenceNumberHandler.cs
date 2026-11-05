using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetBookingReferenceNumberHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SetBookingReferenceNumberCommand, Result<string>>
{
    public async Task<Result<string>> Handle(SetBookingReferenceNumberCommand req, CancellationToken ct)
    {
        if (!tenantUow.GetCurrentTenant().Settings.ColdChainBookingNumbersTyped)
        {
            return Result<string>.Fail("TrackWise issues the booking numbers, so they can't be typed in.");
        }

        var booking = await tenantUow.Repository<Booking>().GetByIdAsync(req.Id, ct);
        if (booking is null)
        {
            return Result<string>.Fail($"Booking with ID '{req.Id}' not found.");
        }

        try
        {
            var reference = await ColdChainBookingNumbers.NormalizeFreeAsync(tenantUow, req.ReferenceNumber, booking.Id, ct);
            booking.SetReferenceNumber(reference);
            await tenantUow.SaveChangesAsync(ct);
            return Result<string>.Ok(reference);
        }
        catch (InvalidOperationException ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }
}
