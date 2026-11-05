using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Commands;

internal sealed class CreateBillingBatchHandler(ITenantUnitOfWork tenantUow, IWorkfileReferenceGenerator references)
    : IAppRequestHandler<CreateBillingBatchCommand, Result<BillingBatchDto>>
{
    public async Task<Result<BillingBatchDto>> Handle(CreateBillingBatchCommand req, CancellationToken ct)
    {
        var customer = await tenantUow.Repository<Customer>().GetByIdAsync(req.CustomerId, ct);
        if (customer is null)
        {
            return Result<BillingBatchDto>.Fail($"Could not find customer with ID '{req.CustomerId}'.");
        }

        var period = new BillingPeriod(req.PeriodStart, req.PeriodEnd);
        var trips = await BillableTripReader.InPeriodAsync(tenantUow, customer.Id, period, null, ct);

        if (req.TripIds.Count > 0)
        {
            var available = trips.Select(t => t.TripId).ToHashSet();
            var unavailable = req.TripIds.Count(id => !available.Contains(id));
            if (unavailable > 0)
            {
                return Result<BillingBatchDto>.Fail(
                    $"{unavailable} of the chosen trips aren't completed {customer.Name} trips in this period, or are on another batch already.");
            }

            trips = trips.Where(t => req.TripIds.Contains(t.TripId)).ToList();
        }

        if (trips.Count == 0)
        {
            return Result<BillingBatchDto>.Fail(
                $"{customer.Name} has no completed trips from {period.Start:MMM d} to {period.End:MMM d, yyyy} that aren't on a batch already.");
        }

        // Taken only once there is something to bill, so a refused request doesn't use up a number.
        var reference = await references.NextAsync(WorkfileSeries.ReeferBilling, ct);

        try
        {
            var currency = tenantUow.GetCurrentTenant().Settings.Currency.ToString();
            var batch = BillingBatch.Create(reference, customer.Id, period, currency);
            batch.Notes = req.Notes;
            batch.SyncTrips(trips, includeNew: true);

            await tenantUow.Repository<BillingBatch>().AddAsync(batch, ct);
            await tenantUow.SaveChangesAsync(ct);
            return Result<BillingBatchDto>.Ok(await BillingBatchReader.ToDtoAsync(tenantUow, batch, ct));
        }
        catch (InvalidOperationException ex)
        {
            return Result<BillingBatchDto>.Fail(ex.Message);
        }
    }
}
