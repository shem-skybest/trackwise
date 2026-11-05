using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Queries;

internal sealed class GetPendingDvirReviewsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetPendingDvirReviewsQuery, Result<List<DvirReportDto>>>
{
    public async Task<Result<List<DvirReportDto>>> Handle(GetPendingDvirReviewsQuery req, CancellationToken ct)
    {
        var reports = await tenantUow.Repository<DvirReport>().GetListAsync(
            r => r.Status == DvirStatus.Submitted || r.Status == DvirStatus.RequiresRepair,
            ct);

        var truckNumbers = await TruckRefResolver.ResolveAsync(
            tenantUow, reports.Select(r => r.TruckId).Distinct().ToArray(), ct);

        var dtos = reports
            .OrderBy(r => r.InspectionDate)
            .Select(r => r.ToDto(
                truckNumbers.TryGetValue(r.TruckId, out var truckRef) ? truckRef.Number : null))
            .ToList();

        return Result<List<DvirReportDto>>.Ok(dtos);
    }
}
