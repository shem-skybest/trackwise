using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

internal sealed class GetTripCorrectionsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetTripCorrectionsQuery, Result<List<TripCorrectionDto>>>
{
    public async Task<Result<List<TripCorrectionDto>>> Handle(GetTripCorrectionsQuery req, CancellationToken ct)
    {
        var corrections = await tenantUow.Repository<TripCorrection>().Query()
            .Where(c => c.TripId == req.TripId)
            .OrderByDescending(c => c.CorrectedAt)
            .ThenBy(c => c.Field)
            .ToListAsync(ct);

        return Result<List<TripCorrectionDto>>.Ok(corrections.Select(c => c.ToDto()).ToList());
    }
}
