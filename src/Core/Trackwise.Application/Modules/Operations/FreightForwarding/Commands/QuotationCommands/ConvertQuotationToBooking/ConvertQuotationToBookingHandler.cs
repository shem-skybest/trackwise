using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ConvertQuotationToBooking;

internal sealed class ConvertQuotationToBookingHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ConvertQuotationToBookingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ConvertQuotationToBookingCommand req, CancellationToken ct)
    {
        var quotation = await tenantUow.Repository<Quotation>().GetByIdAsync(req.QuotationId, ct);
        if (quotation is null)
        {
            return Result<Guid>.Fail($"Quotation with ID '{req.QuotationId}' not found.");
        }

        if (quotation.Status != QuotationStatus.Accepted)
        {
            return Result<Guid>.Fail("Only Accepted quotations can be converted to a Booking.");
        }

        if (!req.Repeat)
        {
            var existingBooking = await tenantUow.Repository<Booking>().Query()
                .Where(b => b.QuotationId == req.QuotationId)
                .Select(b => (Guid?)b.Id)
                .FirstOrDefaultAsync(ct);

            if (existingBooking.HasValue)
                return Result<Guid>.Ok(existingBooking.Value);
        }

        var existingCount = await tenantUow.Repository<Booking>().Query()
            .IgnoreQueryFilters()
            .CountAsync(b => b.QuotationId == quotation.Id, ct);

        var booking = Booking.CreateDraftFromQuotation(
            quotation,
            bookingSequence: existingCount + 1,
            requestedPickupDate: req.RequestedPickupDate,
            requestedDeliveryDate: req.RequestedDeliveryDate,
            notes: req.Notes);

        await tenantUow.Repository<Booking>().AddAsync(booking, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(booking.Id);
    }
}
