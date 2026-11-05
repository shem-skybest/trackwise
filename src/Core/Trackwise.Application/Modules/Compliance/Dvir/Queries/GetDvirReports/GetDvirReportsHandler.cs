using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Compliance.Dvir.Queries;

internal sealed class GetDvirReportsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetDvirReportsQuery, PagedResult<DvirReportDto>>
{
    public async Task<PagedResult<DvirReportDto>> Handle(GetDvirReportsQuery req, CancellationToken ct)
    {
        var baseQuery = tenantUow.Repository<DvirReport>().Query();

        if (req.TruckId.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.TruckId == req.TruckId);
        }

        if (req.DriverId.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.DriverId == req.DriverId);
        }

        if (req.Status.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.Status == req.Status);
        }

        if (req.Type.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.Type == req.Type);
        }

        if (req.FromDate.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.InspectionDate >= req.FromDate);
        }

        if (req.ToDate.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.InspectionDate <= req.ToDate);
        }

        baseQuery = baseQuery.OrderBy(req.OrderBy ?? "-InspectionDate");

        var totalItems = baseQuery.Count();
        baseQuery = baseQuery.ApplyPaging(req.Page, req.PageSize);

        var reports = await baseQuery.ToArrayAsync(ct);

        var truckNumbers = await TruckRefResolver.ResolveAsync(
            tenantUow, reports.Select(r => r.TruckId).Distinct().ToArray(), ct);

        var dtos = reports
            .Select(r => r.ToDto(
                truckNumbers.TryGetValue(r.TruckId, out var truckRef) ? truckRef.Number : null))
            .ToArray();

        return PagedResult<DvirReportDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
