using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

internal sealed class RejectExpenseHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RejectExpenseCommand, Result>
{
    public async Task<Result> Handle(RejectExpenseCommand req, CancellationToken ct)
    {
        var expense = await tenantUow.Repository<Expense>().GetByIdAsync(req.Id);

        if (expense is null)
        {
            return Result.Fail($"Could not find expense with ID '{req.Id}'");
        }

        if (expense.Status != ExpenseStatus.Pending)
        {
            return Result.Fail("Only pending expenses can be rejected.");
        }

        expense.Reject(req.ApproverId, req.Reason);

        tenantUow.Repository<Expense>().Update(expense);
        await tenantUow.SaveChangesAsync();

        return Result.Ok();
    }
}
