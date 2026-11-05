using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.Invoice;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Financial.Invoices.Services;

internal static class BookingInvoiceDraftBuilder
{
    public static async Task<BookingInvoice> CreateOrReconcileAsync(
        ITenantUnitOfWork tenantUow,
        Booking booking,
        BookingInvoice? invoice,
        BookingInvoicePurpose purpose,
        Guid? relatedInvoiceId,
        CancellationToken ct)
    {
        var invoiceCreated = invoice is null;
        if (invoice is not null && invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only a draft booking invoice can be reconciled.");

        if (invoice is null)
        {
            var zero = Money.Zero(booking.Currency);
            invoice = new BookingInvoice
            {
                BookingId = booking.Id,
                Booking = booking,
                CustomerId = booking.CustomerId,
                Subtotal = zero,
                TaxTotal = zero,
                Total = zero,
                Status = InvoiceStatus.Draft,
                DueDate = DateTime.UtcNow.AddDays(booking.PaymentTermsDays),
                Purpose = purpose,
                RelatedInvoiceId = relatedInvoiceId
            };
            await tenantUow.Repository<BookingInvoice>().AddAsync(invoice, ct);
        }

        var billable = booking.Charges
            .Where(c => c.IsBillable &&
                (c.BillingStatus == BillingStatus.Unbilled || c.InvoiceId == invoice.Id))
            .ToDictionary(c => c.Id);

        foreach (var line in invoice.LineItems.ToList())
        {
            if (line.BookingChargeId is Guid chargeId && !billable.ContainsKey(chargeId))
            {
                tenantUow.Repository<InvoiceLineItem>().Delete(line);
                invoice.LineItems.Remove(line);
            }
        }

        foreach (var charge in billable.Values)
        {
            var line = invoice.LineItems.FirstOrDefault(l => l.BookingChargeId == charge.Id);
            if (line is null)
            {
                line = new InvoiceLineItem
                {
                    InvoiceId = invoice.Id,
                    BookingChargeId = charge.Id,
                    Description = charge.Description,
                    Type = ToLineType(charge.Category),
                    Amount = new Money { Amount = charge.SellingAmount, Currency = charge.Currency },
                    Quantity = 1,
                    Order = invoice.LineItems.Count == 0 ? 0 : invoice.LineItems.Max(l => l.Order) + 1,
                    Notes = ChargeNotes(charge)
                };
                await tenantUow.Repository<InvoiceLineItem>().AddAsync(line, ct);
                invoice.LineItems.Add(line);
            }
            else
            {
                line.Description = charge.Description;
                line.Type = ToLineType(charge.Category);
                line.Amount = new Money { Amount = charge.SellingAmount, Currency = charge.Currency };
                line.Notes = ChargeNotes(charge);
            }
        }

        invoice.Subtotal = new Money
        {
            Amount = invoice.LineItems.Sum(l => l.Total),
            Currency = booking.Currency
        };
        invoice.TaxTotal = Money.Zero(booking.Currency);
        invoice.Total = invoice.Subtotal;

        booking.MarkChargesInvoiced(
            invoice.Id,
            billable.Values
                .Where(c => c.BillingStatus == BillingStatus.Unbilled)
                .Select(c => c.Id));

        if (!invoiceCreated)
            tenantUow.Repository<BookingInvoice>().Update(invoice);

        return invoice;
    }

    private static string ChargeNotes(BookingChargeLine charge) =>
        charge.Reason is null
            ? $"Quantity: {charge.Quantity}"
            : $"{charge.Reason} (Quantity: {charge.Quantity})";

    private static InvoiceLineItemType ToLineType(ChargeCategory category) => category switch
    {
        ChargeCategory.Base => InvoiceLineItemType.BaseRate,
        ChargeCategory.Accessorial => InvoiceLineItemType.Accessorial,
        ChargeCategory.Surcharge => InvoiceLineItemType.Surcharge,
        ChargeCategory.Tax => InvoiceLineItemType.Tax,
        ChargeCategory.Discount => InvoiceLineItemType.Discount,
        _ => InvoiceLineItemType.Other
    };
}
