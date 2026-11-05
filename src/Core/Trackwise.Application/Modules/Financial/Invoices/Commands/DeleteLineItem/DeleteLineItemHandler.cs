using Trackwise.Application.Modules.Financial.Tax.Services;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

internal sealed class DeleteLineItemHandler(
    ITenantUnitOfWork tenantUow,
    IInvoiceTaxApplier taxApplier)
    : IAppRequestHandler<DeleteLineItemCommand, Result>
{
    public async Task<Result> Handle(DeleteLineItemCommand req, CancellationToken ct)
    {
        var invoice = await tenantUow.Repository<Invoice>().GetByIdAsync(req.InvoiceId, ct);
        if (invoice is null)
        {
            return Result.Fail("Invoice not found.");
        }

        if (invoice is BookingInvoice && invoice.Status != InvoiceStatus.Draft)
            return Result.Fail("Booking invoices can only be modified while in Draft.");

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Result.Fail("Cannot modify a paid invoice.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return Result.Fail("Cannot modify a cancelled invoice.");
        }

        var lineItem = invoice.LineItems.FirstOrDefault(li => li.Id == req.LineItemId);
        if (lineItem is null)
        {
            return Result.Fail("Line item not found.");
        }

        if (invoice is BookingInvoice && lineItem.BookingChargeId.HasValue)
            return Result.Fail("Change booking charges on the Booking; linked invoice lines follow those charges.");

        tenantUow.Repository<InvoiceLineItem>().Delete(lineItem);
        invoice.LineItems.Remove(lineItem);

        await taxApplier.ApplyAsync(invoice, ct);
        tenantUow.Repository<Invoice>().Update(invoice);

        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
