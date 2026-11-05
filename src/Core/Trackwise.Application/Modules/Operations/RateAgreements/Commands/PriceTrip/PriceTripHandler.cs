using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class PriceTripHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<PriceTripCommand, Result<TripRateQuoteDto>>
{
    public async Task<Result<TripRateQuoteDto>> Handle(PriceTripCommand req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result<TripRateQuoteDto>.Fail($"Could not find trip with ID '{req.TripId}'");
        }

        var lockedOn = await tenantUow.Repository<BillingBatchTrip>().Query()
            .Where(l => l.TripId == trip.Id)
            .Join(tenantUow.Repository<BillingBatch>().Query(), l => l.BillingBatchId, b => b.Id, (_, b) => b)
            .Where(b => b.Status != BillingBatchStatus.Draft)
            .Select(b => b.ReferenceNumber)
            .FirstOrDefaultAsync(ct);
        if (lockedOn is not null)
        {
            return Result<TripRateQuoteDto>.Fail(
                $"Trip {trip.Number} is billed on {lockedOn}, which is locked. Unlock the batch to price the trip again.");
        }

        var quoted = await TripRateCalculator.QuoteAsync(tenantUow, trip, req.RatedOn, ct);
        if (!quoted.IsSuccess)
        {
            return Result<TripRateQuoteDto>.Fail(quoted.Error!);
        }

        var (quote, ratedOn, dto) = quoted.Value!;

        try
        {
            trip.ApplyRate(quote, ratedOn, DateTime.UtcNow);
            await tenantUow.SaveChangesAsync(ct);
            return Result<TripRateQuoteDto>.Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return Result<TripRateQuoteDto>.Fail(ex.Message);
        }
    }
}
