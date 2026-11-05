using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

internal sealed class ApproveExpenseHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<ApproveExpenseCommand, Result>
{
    public async Task<Result> Handle(ApproveExpenseCommand req, CancellationToken ct)
    {
        var expense = await tenantUow.Repository<Expense>().GetByIdAsync(req.Id, ct);

        if (expense is null)
        {
            return Result.Fail($"Could not find expense with ID '{req.Id}'");
        }

        if (expense.Status != ExpenseStatus.Pending)
        {
            return Result.Fail("Only pending expenses can be approved.");
        }

        expense.Approve(req.ApproverId);

        tenantUow.Repository<Expense>().Update(expense);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
