using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Commands;

internal sealed class UpdateFuelFillUpHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateFuelFillUpCommand, Result<FuelFillUpDto>>
{
    public async Task<Result<FuelFillUpDto>> Handle(UpdateFuelFillUpCommand req, CancellationToken ct)
    {
        if (await tenantUow.Repository<Expense>().GetByIdAsync(req.Id, ct) is not TruckExpense
            {
                Category: TruckExpenseCategory.Fuel
            } expense)
        {
            return Result<FuelFillUpDto>.Fail($"Could not find the fill-up '{req.Id}'.");
        }

        if (expense.Status is ExpenseStatus.Approved or ExpenseStatus.Paid)
        {
            return Result<FuelFillUpDto>.Fail("A verified fill-up can't be changed.");
        }

        var fillUp = await FuelFillUpChecks.ResolveAsync(tenantUow, req, expense.Id, ct);
        if (!fillUp.IsSuccess)
        {
            return Result<FuelFillUpDto>.Fail(fillUp.Error!);
        }

        expense.CorrectFuel(fillUp.Value!);
        if (!string.IsNullOrEmpty(req.ReceiptBlobPath))
        {
            expense.ReceiptBlobPath = req.ReceiptBlobPath;
        }

        tenantUow.Repository<Expense>().Update(expense);
        await tenantUow.SaveChangesAsync(ct);

        var dtos = await FuelLogReader.ToDtosAsync(tenantUow, [expense], ct);
        return Result<FuelFillUpDto>.Ok(dtos[0]);
    }
}
