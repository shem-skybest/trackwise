using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.UpdateBookingCharge;

internal sealed class UpdateBookingChargeHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateBookingChargeCommand, Result<BookingChargeDto>>
{
    public async Task<Result<BookingChargeDto>> Handle(UpdateBookingChargeCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().Query()
            .Include(b => b.Charges)
            .FirstOrDefaultAsync(b => b.Id == req.BookingId, ct);

        if (booking is null)
            return Result<BookingChargeDto>.Fail("Booking not found.", ErrorCodes.NotFound);

        var charge = booking.Charges.FirstOrDefault(c => c.Id == req.ChargeId);
        if (charge is null)
            return Result<BookingChargeDto>.Fail("Charge not found.", ErrorCodes.NotFound);

        var linkedInvoice = charge.InvoiceId is Guid invoiceId
            ? await tenantUow.Repository<BookingInvoice>().GetAsync(
                i => i.Id == invoiceId && i.BookingId == booking.Id,
                ct)
            : null;

        try
        {
            booking.UpdateAdditionalCharge(
                req.ChargeId, req.Description, req.Quantity, req.UnitBuying, req.UnitSelling,
                req.Reason, linkedInvoice?.Status == InvoiceStatus.Draft);
        }
        catch (InvalidOperationException ex)
        {
            return Result<BookingChargeDto>.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result<BookingChargeDto>.Ok(charge.ToChargeDto());
    }
}
