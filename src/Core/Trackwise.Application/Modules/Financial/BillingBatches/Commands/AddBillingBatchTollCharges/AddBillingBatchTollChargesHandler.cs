using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class AddBillingBatchTollChargesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<AddBillingBatchTollChargesCommand, Result<BillingBatchDto>>
{
    private static readonly CashExpenseCategory[] AtCost =
        [CashExpenseCategory.Toll, CashExpenseCategory.Parking, CashExpenseCategory.Passway];

    public Task<Result<BillingBatchDto>> Handle(AddBillingBatchTollChargesCommand req, CancellationToken ct) =>
        BillingBatchEdit.ApplyAsync(tenantUow, req.Id, async batch =>
        {
            var tripIds = batch.Trips.Select(t => t.TripId).ToList();
            var billed = tenantUow.Repository<BillingBatchCharge>().Query()
                .Where(c => c.CashFundExpenseId != null)
                .Select(c => c.CashFundExpenseId);

            var receipts = await tenantUow.Repository<CashFundExpense>().Query()
                .Where(e => e.TripId != null && tripIds.Contains(e.TripId.Value))
                .Where(e => AtCost.Contains(e.Category) && e.Status != CashExpenseStatus.Disallowed)
                .Where(e => !billed.Contains(e.Id))
                .OrderBy(e => e.SpentOn)
                .Select(e => new { e.Id, e.TripId, e.Category, e.Description, e.Amount })
                .ToListAsync(ct);

            if (receipts.Count == 0)
            {
                throw new InvalidOperationException(
                    "No tolls, parking or passways on this batch's trips are left to bill. Link the cash fund receipts to their trips first.");
            }

            foreach (var receipt in receipts)
            {
                var description = $"{receipt.Category.GetDescription()}: {receipt.Description}";
                var charge = batch.AddCharge(new BillingChargeSpec(
                    BillingChargeKind.TollsAndParking,
                    description[..Math.Min(description.Length, 200)],
                    receipt.Amount,
                    IsVatable: false,
                    receipt.TripId,
                    receipt.Id));
                await tenantUow.Repository<BillingBatchCharge>().AddAsync(charge, ct);
            }
        }, ct);
}
