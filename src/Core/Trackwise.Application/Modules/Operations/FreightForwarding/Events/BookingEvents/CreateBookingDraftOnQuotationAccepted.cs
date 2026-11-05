using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Notifications;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Events.BookingEvents;

// ── 1. Quotation Accepted -> Booking Draft ───────────────────────────────────

public class CreateBookingDraftOnQuotationAccepted(
    ITenantUnitOfWork _tenantUow) : INotificationHandler<QuotationAccepted>
{

    public async Task Handle(QuotationAccepted e, CancellationToken ct)
    {
        var bookingRepo = _tenantUow.Repository<Booking>();
        var quotationRepo = _tenantUow.Repository<Quotation>();

        // Idempotent: one booking per quotation (also enforced by the unique index)
        var alreadyExists = await bookingRepo.Query()
            .AnyAsync(b => b.QuotationId == e.Id, ct);
        if (alreadyExists) return;

        // Lines are required: their issued price snapshots become the booking's locked charges
        var quotation = await quotationRepo.Query()
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == e.Id, ct)
            ?? throw new InvalidOperationException($"Quotation {e.Id} not found.");

        var booking = Booking.CreateDraftFromQuotation(quotation);

        await bookingRepo.AddAsync(booking, ct);
    }
}
