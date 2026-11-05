using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.RateAgreements;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class EstimateColdChainBookingHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<EstimateColdChainBookingQuery, Result<BookingEstimateDto>>
{
    public async Task<Result<BookingEstimateDto>> Handle(EstimateColdChainBookingQuery req, CancellationToken ct)
    {
        var linkedSiteIds = new HashSet<Guid>();
        if (req.BookingId is { } bookingId
            && await tenantUow.Repository<Booking>().GetByIdAsync(bookingId, ct) is { } booking)
        {
            linkedSiteIds.UnionWith(booking.Drops.Select(d => d.SiteId));
            if (booking.PickupSiteId is { } pickupId)
            {
                linkedSiteIds.Add(pickupId);
            }
        }

        var plan = req.ToPlan();
        var prepared = await BookingRateEstimator.PrepareAsync(tenantUow, req.CustomerId, plan, linkedSiteIds, ct);
        if (!prepared.IsSuccess)
        {
            return Result<BookingEstimateDto>.Ok(new BookingEstimateDto { Error = prepared.Error });
        }

        var (_, estimate) = await BookingRateEstimator.EstimateAsync(tenantUow, prepared.Value!, plan, ct);
        return Result<BookingEstimateDto>.Ok(estimate);
    }
}
