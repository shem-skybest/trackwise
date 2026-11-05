using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Commands;

internal sealed class RecordFuelFillUpHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RecordFuelFillUpCommand, Result<FuelFillUpDto>>
{
    public async Task<Result<FuelFillUpDto>> Handle(RecordFuelFillUpCommand req, CancellationToken ct)
    {
        var fillUp = await FuelFillUpChecks.ResolveAsync(tenantUow, req, null, ct);
        if (!fillUp.IsSuccess)
        {
            return Result<FuelFillUpDto>.Fail(fillUp.Error!);
        }

        var expense = TruckExpense.ForFuel(fillUp.Value!);
        expense.ReceiptBlobPath = req.ReceiptBlobPath;

        await tenantUow.Repository<Expense>().AddAsync(expense, ct);
        await tenantUow.SaveChangesAsync(ct);

        var dtos = await FuelLogReader.ToDtosAsync(tenantUow, [expense], ct);
        return Result<FuelFillUpDto>.Ok(dtos[0]);
    }
}
