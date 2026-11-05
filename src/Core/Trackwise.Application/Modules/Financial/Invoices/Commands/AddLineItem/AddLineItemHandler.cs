using Trackwise.Application.Modules.Financial.Tax.Services;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

internal sealed class AddLineItemHandler(
    ITenantUnitOfWork tenantUow,
    IInvoiceTaxApplier taxApplier)
    : IAppRequestHandler<AddLineItemCommand, Result<InvoiceLineItemDto>>
{
    public async Task<Result<InvoiceLineItemDto>> Handle(AddLineItemCommand req, CancellationToken ct)
    {
        var invoice = await tenantUow.Repository<Invoice>().GetByIdAsync(req.InvoiceId, ct);
        if (invoice is null)
        {
            return Result<InvoiceLineItemDto>.Fail("Invoice not found.");
        }

        if (invoice is BookingInvoice && invoice.Status != InvoiceStatus.Draft)
            return Result<InvoiceLineItemDto>.Fail("Booking invoices can only be modified while in Draft.");

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Result<InvoiceLineItemDto>.Fail("Cannot modify a paid invoice.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return Result<InvoiceLineItemDto>.Fail("Cannot modify a cancelled invoice.");
        }

        var nextOrder = invoice.LineItems.Any() ? invoice.LineItems.Max(li => li.Order) + 1 : 0;

        var lineItem = new InvoiceLineItem
        {
            InvoiceId = invoice.Id,
            Description = req.Description,
            Type = req.Type,
            Amount = invoice.Total with { Amount = req.Amount },
            Quantity = req.Quantity,
            Order = nextOrder,
            Notes = req.Notes
        };

        await tenantUow.Repository<InvoiceLineItem>().AddAsync(lineItem, ct);
        invoice.LineItems.Add(lineItem);

        await taxApplier.ApplyAsync(invoice, ct);
        tenantUow.Repository<Invoice>().Update(invoice);

        await tenantUow.SaveChangesAsync(ct);

        return Result<InvoiceLineItemDto>.Ok(lineItem.ToDto());
    }
}
