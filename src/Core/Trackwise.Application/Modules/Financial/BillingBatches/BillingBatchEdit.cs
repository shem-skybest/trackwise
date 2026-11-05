using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches;

/// <summary>Loads a batch, applies one change, saves and returns it - the shape of every batch command.</summary>
internal static class BillingBatchEdit
{
    public static async Task<Result<BillingBatchDto>> ApplyAsync(
        ITenantUnitOfWork tenantUow, Guid batchId, Func<BillingBatch, Task> change, CancellationToken ct)
    {
        var batch = await tenantUow.Repository<BillingBatch>().GetByIdAsync(batchId, ct);
        if (batch is null)
        {
            return Result<BillingBatchDto>.Fail($"Could not find billing batch with ID '{batchId}'.");
        }

        try
        {
            await change(batch);
            await tenantUow.SaveChangesAsync(ct);
            return Result<BillingBatchDto>.Ok(await BillingBatchReader.ToDtoAsync(tenantUow, batch, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<BillingBatchDto>.Fail(ex.Message);
        }
    }

    public static Task<Result<BillingBatchDto>> ApplyAsync(
        ITenantUnitOfWork tenantUow, Guid batchId, Action<BillingBatch> change, CancellationToken ct) =>
        ApplyAsync(tenantUow, batchId, batch =>
        {
            change(batch);
            return Task.CompletedTask;
        }, ct);
}
