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

        var alreadyBooked = await tenantUow.Repository<Booking>().Query()
            .AnyAsync(b => b.QuotationId == req.QuotationId, ct);
        if (alreadyBooked)
        {
            return Result<Guid>.Fail("This Quotation has already been converted to a Booking.");
        }

        var booking = Booking.CreateDraftFromQuotation(
            quotation
            //req.ContainerTypeId,
            //req.RequestedPickupDate,
            //req.RequestedDeliveryDate,
            //req.Notes
            );

        await tenantUow.Repository<Booking>().AddAsync(booking, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(booking.Id);
    }
}
