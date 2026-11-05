using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Expenses.Commands;

internal sealed class CreateTruckExpenseHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateTruckExpenseCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateTruckExpenseCommand req, CancellationToken ct)
    {
        var truck = await tenantUow.Repository<Truck>().GetByIdAsync(req.TruckId, ct);

        if (truck is null)
        {
            return Result<Guid>.Fail($"Could not find truck with ID '{req.TruckId}'");
        }

        var expense = new TruckExpense
        {
            Amount = new Money { Amount = req.Amount, Currency = req.Currency },
            VendorName = req.VendorName,
            ExpenseDate = req.ExpenseDate,
            ReceiptBlobPath = req.ReceiptBlobPath,
            Notes = req.Notes,
            TruckId = req.TruckId,
            Category = req.Category,
            OdometerReading = req.OdometerReading,
            Quantity = req.Quantity,
            QuantityUnit = req.QuantityUnit
        };

        await tenantUow.Repository<Expense>().AddAsync(expense, ct);
        await tenantUow.SaveChangesAsync(ct);

        return Result<Guid>.Ok(expense.Id);
    }
}
