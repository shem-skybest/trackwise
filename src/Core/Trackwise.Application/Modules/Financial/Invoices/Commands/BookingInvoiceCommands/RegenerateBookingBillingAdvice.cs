using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Financial.Invoices.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Invoice;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands.BookingInvoiceCommands;

public sealed record RegenerateBookingBillingAdviceCommand(Guid BookingId) : ICommand<Result<InvoiceDto>>;

internal sealed class RegenerateBookingBillingAdviceHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RegenerateBookingBillingAdviceCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(RegenerateBookingBillingAdviceCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().Query()
            .Include(b => b.Charges)
            .FirstOrDefaultAsync(b => b.Id == req.BookingId, ct);

        if (booking is null)
            return Result<InvoiceDto>.Fail($"Booking '{req.BookingId}' was not found.", ErrorCodes.NotFound);

        var invoices = await tenantUow.Repository<BookingInvoice>().Query()
            .Include(i => i.LineItems)
            .Where(i => i.BookingId == booking.Id)
            .OrderByDescending(i => i.Number)
            .ToListAsync(ct);

        var cancelledInvoice = invoices.FirstOrDefault(i => i.Status == InvoiceStatus.Cancelled);
        if (cancelledInvoice is null)
            return Result<InvoiceDto>.Fail("Billing advice can only be regenerated after a booking invoice is cancelled.");

        var replacement = invoices.FirstOrDefault(i =>
            i.Status == InvoiceStatus.Draft &&
            i.Purpose == BookingInvoicePurpose.Replacement &&
            i.RelatedInvoiceId == cancelledInvoice.Id);

        if (replacement is not null)
            return Result<InvoiceDto>.Ok(replacement.ToDto());

        if (invoices.Any(i => i.Status == InvoiceStatus.Draft))
            return Result<InvoiceDto>.Fail("Resolve the existing draft invoice before regenerating billing advice.");

        if (booking.GetUnbilledCharges().Count == 0)
            return Result<InvoiceDto>.Fail("There are no unbilled charges to include in the regenerated billing advice.");

        replacement = await BookingInvoiceDraftBuilder.CreateOrReconcileAsync(
            tenantUow,
            booking,
            invoice: null,
            BookingInvoicePurpose.Replacement,
            cancelledInvoice.Id,
            ct);

        await tenantUow.SaveChangesAsync(ct);
        return Result<InvoiceDto>.Ok(replacement.ToDto());
    }
}
