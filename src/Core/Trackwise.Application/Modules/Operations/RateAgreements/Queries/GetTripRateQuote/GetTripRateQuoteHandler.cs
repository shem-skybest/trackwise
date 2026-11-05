using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Queries;

internal sealed class GetTripRateQuoteHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTripRateQuoteQuery, Result<TripRateQuoteDto>>
{
    public async Task<Result<TripRateQuoteDto>> Handle(GetTripRateQuoteQuery req, CancellationToken ct)
    {
        var trip = await tenantUow.Repository<Trip>().GetByIdAsync(req.TripId, ct);
        if (trip is null)
        {
            return Result<TripRateQuoteDto>.Fail($"Could not find trip with ID '{req.TripId}'");
        }

        var quoted = await TripRateCalculator.QuoteAsync(tenantUow, trip, req.RatedOn, ct);

        return quoted.IsSuccess
            ? Result<TripRateQuoteDto>.Ok(quoted.Value.Dto)
            : Result<TripRateQuoteDto>.Fail(quoted.Error!);
    }
}
