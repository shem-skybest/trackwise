using Trackwise.Domain.Entities;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

internal sealed class DeleteInvoiceHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteInvoiceCommand, Result>
{
    public async Task<Result> Handle(DeleteInvoiceCommand req, CancellationToken ct)
    {
        var invoice = await tenantUow.Repository<Invoice>().GetByIdAsync(req.Id, ct);
        if (invoice is null)
            return Result.Fail($"Could not find an invoice with ID '{req.Id}'");

        if (invoice is BookingInvoice)
            return Result.Fail("Booking invoices are retained for audit. Cancel an eligible draft instead of deleting it.");

        tenantUow.Repository<Invoice>().Delete(invoice);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
