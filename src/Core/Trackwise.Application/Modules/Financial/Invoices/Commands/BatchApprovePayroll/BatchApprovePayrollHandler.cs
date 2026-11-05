using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Trackwise.Application.Abstractions.CurrentUser;

namespace Trackwise.Application.Modules.Financial.Invoices.Commands;

internal sealed class BatchApprovePayrollHandler(
    ITenantUnitOfWork tenantUow,
    ICurrentUserService currentUserService)
    : IAppRequestHandler<BatchApprovePayrollCommand, Result>
{
    public async Task<Result> Handle(BatchApprovePayrollCommand req, CancellationToken ct)
    {
        var currentUserId = currentUserService.GetUserId();
        if (currentUserId is null)
        {
            return Result.Fail("Unable to determine the current user");
        }

        var payrolls = await tenantUow.Repository<PayrollInvoice>()
            .GetListAsync(p => req.Ids.Contains(p.Id), ct);

        if (payrolls.Count == 0)
        {
            return Result.Fail("No payroll invoices found with the provided IDs");
        }

        foreach (var payroll in payrolls)
        {
            if (payroll.Status != InvoiceStatus.PendingApproval)
            {
                continue;
            }

            payroll.Status = InvoiceStatus.Approved;
            payroll.ApprovedById = currentUserId.Value;
            payroll.ApprovedAt = DateTime.UtcNow;
            payroll.ApprovalNotes = req.Notes;

            tenantUow.Repository<PayrollInvoice>().Update(payroll);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
