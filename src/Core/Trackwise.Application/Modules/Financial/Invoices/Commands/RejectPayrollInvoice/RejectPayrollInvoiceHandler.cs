using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

internal sealed class RejectPayrollInvoiceHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUserService)
    : IAppRequestHandler<RejectPayrollInvoiceCommand, Result>
{
    public async Task<Result> Handle(RejectPayrollInvoiceCommand req, CancellationToken ct)
    {
        var payroll = await tenantUow.Repository<PayrollInvoice>().GetByIdAsync(req.Id);

        if (payroll is null)
        {
            return Result.Fail($"Could not find payroll invoice with ID '{req.Id}'");
        }

        if (payroll.Status != InvoiceStatus.PendingApproval)
        {
            return Result.Fail("Only payroll invoices pending approval can be rejected");
        }

        var currentUserId = currentUserService.GetUserId() ?? Guid.Empty;
        payroll.Reject(currentUserId, req.Reason);
        tenantUow.Repository<PayrollInvoice>().Update(payroll);
        await tenantUow.SaveChangesAsync();

        return Result.Ok();
    }
}
