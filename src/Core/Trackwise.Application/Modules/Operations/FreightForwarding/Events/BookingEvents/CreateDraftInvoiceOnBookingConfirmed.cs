using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Financial.Invoices.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Invoice;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Events.BookingEvents;

// ── 2. Booking Confirmed -> Billing draft invoice (Billing module) ───────────

internal sealed class CreateDraftInvoiceOnBookingConfirmed(ITenantUnitOfWork tenantUow)
    : IDomainEventHandler<BookingConfirmed>,
      IDomainEventHandler<BookingChargeAdded>,
      IDomainEventHandler<BookingChargeUpdated>,
      IDomainEventHandler<BookingChargeRemoved>
{
    public Task Handle(BookingConfirmed @event, CancellationToken ct) => Reconcile(@event.Id, ct);
    public Task Handle(BookingChargeAdded @event, CancellationToken ct) => Reconcile(@event.Id, ct);
    public Task Handle(BookingChargeUpdated @event, CancellationToken ct) => Reconcile(@event.Id, ct);
    public Task Handle(BookingChargeRemoved @event, CancellationToken ct) => Reconcile(@event.Id, ct);

    private async Task Reconcile(Guid bookingId, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().Query()
            .Include(b => b.Charges)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        var invoices = await tenantUow.Repository<BookingInvoice>().Query()
            .Include(i => i.LineItems)
            .Where(i => i.BookingId == bookingId)
            .OrderByDescending(i => i.Number)
            .ToListAsync(ct);

        var draft = invoices.FirstOrDefault(i => i.Status == InvoiceStatus.Draft);
        if (draft is not null)
        {
            await BookingInvoiceDraftBuilder.CreateOrReconcileAsync(
                tenantUow, booking, draft, draft.Purpose, draft.RelatedInvoiceId, ct);
        }
        else
        {
            if (booking.Status == Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums.BookingStatus.Draft ||
                booking.GetUnbilledCharges().Count == 0)
                return;

            if (invoices.FirstOrDefault()?.Status is InvoiceStatus.Cancelled or InvoiceStatus.Rejected)
                return;

            var latestActiveInvoice = invoices.FirstOrDefault(i => i.Status != InvoiceStatus.Cancelled);
            if (latestActiveInvoice is null && invoices.Count > 0)
                return;

            var purpose = latestActiveInvoice is null
                ? BookingInvoicePurpose.Original
                : BookingInvoicePurpose.Supplemental;

            await BookingInvoiceDraftBuilder.CreateOrReconcileAsync(
                tenantUow,
                booking,
                invoice: null,
                purpose,
                latestActiveInvoice?.Id,
                ct);
        }

        await tenantUow.SaveChangesAsync(ct);
    }
}
