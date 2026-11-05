using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Queries;

internal sealed class GetLaneRateFloorsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetLaneRateFloorsQuery, Result<List<LaneRateFloorDto>>>
{
    public async Task<Result<List<LaneRateFloorDto>>> Handle(GetLaneRateFloorsQuery req, CancellationToken ct)
    {
        var floors = await tenantUow.Repository<LaneRateFloor>().GetListAsync(ct: ct);
        var dtos = floors.Select(f => f.ToDto()).ToList();
        return Result<List<LaneRateFloorDto>>.Ok(dtos);
    }
}
