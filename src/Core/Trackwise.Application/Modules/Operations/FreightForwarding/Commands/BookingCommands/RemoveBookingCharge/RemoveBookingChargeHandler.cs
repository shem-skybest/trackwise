using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.RemoveBookingCharge;

/// <summary>Removes an ADDITIONAL charge that has not been invoiced. Quoted charges are locked.</summary>

internal sealed class RemoveBookingChargeHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RemoveBookingChargeCommand, Result>
{
    public async Task<Result> Handle(RemoveBookingChargeCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().Query()
            .Include(b => b.Charges)
            .FirstOrDefaultAsync(b => b.Id == req.BookingId, ct);

        if (booking is null)
            return Result.Fail($"Booking with ID '{req.BookingId}' not found.", ErrorCodes.NotFound);

        // RemoveAdditionalCharge ignores an unknown id, so check first to give a real 404.
        if (booking.Charges.All(c => c.Id != req.ChargeId))
            return Result.Fail($"Charge '{req.ChargeId}' not found on this booking.", ErrorCodes.NotFound);

        var charge = booking.Charges.First(c => c.Id == req.ChargeId);
        try
        {
            var linkedInvoice = charge.InvoiceId is Guid invoiceId
                ? await tenantUow.Repository<BookingInvoice>().GetAsync(
                    i => i.Id == invoiceId && i.BookingId == booking.Id,
                    ct)
                : null;
            booking.RemoveAdditionalCharge(req.ChargeId, linkedInvoice?.Status == InvoiceStatus.Draft);
        }
        catch (InvalidOperationException ex)
        {
            // Quoted charge, or already invoiced.
            return Result.Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
