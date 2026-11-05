using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Invoice;

namespace Trackwise.Domain.Entities;

public class BookingInvoice : Invoice
{
    public override InvoiceType Type { get; set; } = InvoiceType.Booking;
    public required Guid BookingId { get; set; }
    public virtual Booking Booking { get; set; } = null!;

    public required Guid CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public BookingInvoicePurpose Purpose { get; set; } = BookingInvoicePurpose.Original;
    public Guid? RelatedInvoiceId { get; set; }
    public virtual BookingInvoice? RelatedInvoice { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
}
