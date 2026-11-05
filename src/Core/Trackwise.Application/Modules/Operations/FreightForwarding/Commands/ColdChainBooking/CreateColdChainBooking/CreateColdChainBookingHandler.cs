using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.RateAgreements;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CreateColdChainBookingHandler(
    ITenantUnitOfWork tenantUow,
    IWorkfileReferenceGenerator referenceGenerator)
    : IAppRequestHandler<CreateColdChainBookingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateColdChainBookingCommand req, CancellationToken ct)
    {
        var plan = req.ToPlan();

        var prepared = await BookingRateEstimator.PrepareAsync(
            tenantUow, req.CustomerId, plan, new HashSet<Guid>(), ct);
        if (!prepared.IsSuccess)
        {
            return Result<Guid>.Fail(prepared.Error!);
        }

        var context = prepared.Value!;

        try
        {
            var reference = await ColdChainBookingNumbers.ForNewBookingAsync(
                tenantUow, referenceGenerator, req.ReferenceNumber, ct);

            var booking = Booking.CreateFromRateAgreement(context.Agreement, reference, plan);

            var (quote, estimate) = await BookingRateEstimator.EstimateAsync(tenantUow, context, plan, ct);
            if (quote is not null)
            {
                booking.ApplyEstimate(quote);
            }
            else
            {
                booking.ClearEstimate(estimate.Error!);
            }

            await tenantUow.Repository<Booking>().AddAsync(booking, ct);
            await tenantUow.SaveChangesAsync(ct);
            return Result<Guid>.Ok(booking.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Fail(ex.Message);
        }
    }
}
