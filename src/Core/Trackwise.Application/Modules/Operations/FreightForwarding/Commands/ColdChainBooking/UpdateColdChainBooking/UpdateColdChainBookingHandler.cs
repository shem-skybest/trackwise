using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.RateAgreements;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class UpdateColdChainBookingHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateColdChainBookingCommand, Result>
{
    public async Task<Result> Handle(UpdateColdChainBookingCommand req, CancellationToken ct)
    {
        var booking = await tenantUow.Repository<Booking>().GetByIdAsync(req.Id, ct);
        if (booking is null)
        {
            return Result.Fail($"Booking with ID '{req.Id}' not found.");
        }

        if (!booking.IsFromRateAgreement)
        {
            return Result.Fail("Only a Cold Chain booking can be edited here.");
        }

        var plan = req.ToPlan();
        var linkedSiteIds = booking.Drops.Select(d => d.SiteId)
            .Concat(booking.PickupSiteId is { } pickupId ? [pickupId] : [])
            .ToHashSet();

        var prepared = await BookingRateEstimator.PrepareAsync(tenantUow, booking.CustomerId, plan, linkedSiteIds, ct);
        if (!prepared.IsSuccess)
        {
            return Result.Fail(prepared.Error!);
        }

        var context = prepared.Value!;

        try
        {
            var dropRepository = tenantUow.Repository<BookingDrop>();
            foreach (var old in booking.Drops.ToList())
            {
                dropRepository.Delete(old);
            }

            // The booking is tracked, so drops reaching it only through the collection would be
            // saved as updates to rows that don't exist.
            foreach (var drop in booking.UpdatePlan(context.Agreement, plan))
            {
                await dropRepository.AddAsync(drop, ct);
            }

            var (quote, estimate) = await BookingRateEstimator.EstimateAsync(tenantUow, context, plan, ct);
            if (quote is not null)
            {
                booking.ApplyEstimate(quote);
            }
            else
            {
                booking.ClearEstimate(estimate.Error!);
            }

            await tenantUow.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }
    }
}
