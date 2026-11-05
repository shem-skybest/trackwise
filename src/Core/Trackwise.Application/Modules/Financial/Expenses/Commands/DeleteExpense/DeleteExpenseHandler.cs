using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

internal sealed class DeleteExpenseHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteExpenseCommand, Result>
{
    public async Task<Result> Handle(DeleteExpenseCommand req, CancellationToken ct)
    {
        var expense = await tenantUow.Repository<Expense>().GetByIdAsync(req.Id, ct);

        if (expense is null)
        {
            return Result.Fail($"Could not find expense with ID '{req.Id}'");
        }

        // Only pending or rejected expenses can be deleted
        if (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid)
        {
            return Result.Fail("Approved or paid expenses cannot be deleted.");
        }

        tenantUow.Repository<Expense>().Delete(expense);
        await tenantUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
