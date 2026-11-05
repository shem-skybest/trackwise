using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

internal sealed class UpdateInvoiceHandler : IAppRequestHandler<UpdateInvoiceCommand, Result>
{
    private readonly ITenantUnitOfWork _tenantUow;

    public UpdateInvoiceHandler(ITenantUnitOfWork tenantUow)
    {
        _tenantUow = tenantUow;
    }

    public async Task<Result> Handle(
        UpdateInvoiceCommand req, CancellationToken ct)
    {
        var invoice = await _tenantUow.Repository<Invoice>().GetByIdAsync(req.Id);

        if (invoice is null)
        {
            return Result.Fail($"Could not find an invoice with ID '{req.Id}'");
        }

        if (invoice is BookingInvoice)
            return Result.Fail("Booking invoice status must be changed through its lifecycle actions.");

        if (req.InvoiceStatus.HasValue && invoice.Status != req.InvoiceStatus)
        {
            invoice.Status = req.InvoiceStatus.Value;
        }

        _tenantUow.Repository<Invoice>().Update(invoice);
        await _tenantUow.SaveChangesAsync();
        return Result.Ok();
    }
}
