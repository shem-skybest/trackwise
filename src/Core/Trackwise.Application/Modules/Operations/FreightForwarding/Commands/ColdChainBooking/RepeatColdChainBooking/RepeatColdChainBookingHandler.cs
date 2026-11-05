using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.RateAgreements;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class RepeatColdChainBookingHandler(
    ITenantUnitOfWork tenantUow,
    IWorkfileReferenceGenerator referenceGenerator)
    : IAppRequestHandler<RepeatColdChainBookingCommand, Result<List<RepeatedBookingDto>>>
{
    public Task<Result<List<RepeatedBookingDto>>> Handle(RepeatColdChainBookingCommand req, CancellationToken ct) =>
        // A reference number is drawn per copy; a day that fails part-way must not leave the earlier
        // copies saved or their numbers spent.
        tenantUow.ExecuteInTransactionAsync(token => RepeatAsync(req, token), ct);

    private async Task<Result<List<RepeatedBookingDto>>> RepeatAsync(
        RepeatColdChainBookingCommand req, CancellationToken ct)
    {
        var source = await tenantUow.Repository<Booking>().GetByIdAsync(req.BookingId, ct);
        if (source is null)
        {
            return Fail($"Booking with ID '{req.BookingId}' not found.");
        }

        if (!source.IsFromRateAgreement)
        {
            return Fail("Only a Cold Chain booking can be repeated.");
        }

        var repeated = new List<RepeatedBookingDto>();

        try
        {
            foreach (var date in req.Dates.Order())
            {
                var plan = source.PlanOn(date);

                var prepared = await BookingRateEstimator.PrepareAsync(
                    tenantUow, source.CustomerId, plan, new HashSet<Guid>(), ct);
                if (!prepared.IsSuccess)
                {
                    return Fail($"{date:ddd yyyy-MM-dd}: {prepared.Error}");
                }

                // Each copy is its own booking, so while numbers come from nSFS it waits for its own.
                var reference = await ColdChainBookingNumbers.ForNewBookingAsync(tenantUow, referenceGenerator, null, ct);
                var booking = Booking.CreateFromRateAgreement(prepared.Value!.Agreement, reference, plan);

                var (quote, estimate) = await BookingRateEstimator.EstimateAsync(tenantUow, prepared.Value, plan, ct);
                if (quote is not null)
                {
                    booking.ApplyEstimate(quote);
                }
                else
                {
                    booking.ClearEstimate(estimate.Error!);
                }

                if (req.Confirm)
                {
                    booking.Confirm(req.ConfirmedBy);
                }

                await tenantUow.Repository<Booking>().AddAsync(booking, ct);
                repeated.Add(new RepeatedBookingDto
                {
                    Id = booking.Id,
                    ReferenceNumber = reference,
                    ServiceDate = date,
                    Status = booking.Status
                });
            }
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ex.Message);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result<List<RepeatedBookingDto>>.Ok(repeated);
    }

    private static Result<List<RepeatedBookingDto>> Fail(string error) => Result<List<RepeatedBookingDto>>.Fail(error);
}
