using System;
using System.Collections.Generic;
using System.Text;
using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities.FreightForwarding;

/// <summary>
/// One billable line on a Booking. Quoted lines are copied from the issued QuotationLine
/// (price already locked); additional lines are added during execution. Billing builds invoices
/// from these and stamps InvoiceId.
/// </summary>
public class BookingCharge : AuditableEntity, ITenantEntity
{
    public Guid BookingId { get; private set; }
    public virtual Booking? Booking { get; private set; }

    public Guid? QuotationLineId { get; private set; }
    public Guid? TariffRateId { get; private set; }

    public BookingChargeSource Source { get; private set; }
    public ChargeCategory Category { get; private set; }
    public OperationalServiceType ServiceType { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Quantity { get; private set; }
    public string Currency { get; private set; } = default!;

    public decimal BuyingAmount { get; private set; }
    public decimal SellingAmount { get; private set; }

    public Guid? InvoiceId { get; private set; }
    public bool IsInvoiced => InvoiceId.HasValue;

    protected BookingCharge() { }

    internal static BookingCharge FromQuotationLine(Guid bookingId, QuotationLine line, Money buying, Money selling) =>
        new()
        {
            BookingId = bookingId,
            QuotationLineId = line.Id,
            TariffRateId = line.TariffRateId,
            Source = BookingChargeSource.Quoted,
            Category = line.Category,
            ServiceType = line.ServiceType,
            Description = line.TariffRate?.ChargeCode?.Code ?? line.Notes ?? line.Category.ToString(),
            Quantity = line.Quantity,
            Currency = selling.Currency,
            BuyingAmount = buying.Amount,
            SellingAmount = selling.Amount
        };

    internal static BookingCharge Additional(
        Guid bookingId,
        string currency,
        ChargeCategory category,
        OperationalServiceType serviceType,
        string description,
        decimal quantity,
        decimal buyingAmount,
        decimal sellingAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new BookingCharge
        {
            BookingId = bookingId,
            Source = BookingChargeSource.Additional,
            Category = category,
            ServiceType = serviceType,
            Description = description.Trim(),
            Quantity = quantity,
            Currency = currency,
            BuyingAmount = buyingAmount,
            SellingAmount = sellingAmount
        };
    }

    internal void MarkInvoiced(Guid invoiceId)
    {
        if (InvoiceId.HasValue && InvoiceId != invoiceId)
            throw new InvalidOperationException("This charge is already on another invoice.");

        InvoiceId = invoiceId;
    }
}
