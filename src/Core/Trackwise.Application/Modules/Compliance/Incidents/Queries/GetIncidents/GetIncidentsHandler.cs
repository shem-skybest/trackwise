using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Compliance.Incidents.Queries;

internal sealed class GetIncidentsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetIncidentsQuery, PagedResult<IncidentDto>>
{
    public async Task<PagedResult<IncidentDto>> Handle(GetIncidentsQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<Incident>().Query();

        if (req.Kind is { } kind)
        {
            query = query.Where(i => i.Kind == kind);
        }

        if (req.Status is { } status)
        {
            query = query.Where(i => i.Status == status);
        }

        if (req.Severity is { } severity)
        {
            query = query.Where(i => i.Severity == severity);
        }

        if (req.TripId is { } tripId)
        {
            query = query.Where(i => i.TripId == tripId);
        }

        if (req.CustomerId is { } customerId)
        {
            query = query.Where(i => i.CustomerId == customerId);
        }

        if (req.TruckId is { } truckId)
        {
            query = query.Where(i => i.TruckId == truckId);
        }

        if (req.DriverId is { } driverId)
        {
            query = query.Where(i => i.DriverId == driverId);
        }

        if (req.FromDate is { } from)
        {
            query = query.Where(i => i.OccurredAt >= from.ToUniversalTime());
        }

        if (req.ToDate is { } to)
        {
            query = query.Where(i => i.OccurredAt <= to.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            query = query.Where(i =>
                i.Summary.ToLower().Contains(search)
                || (i.Place != null && i.Place.ToLower().Contains(search))
                || i.CaseNumber.ToLower().Contains(search));
        }

        var total = await query.CountAsync(ct);
        var incidents = await query
            .OrderBy(req.OrderBy ?? "-OccurredAt")
            .ApplyPaging(req.Page, req.PageSize)
            .ToListAsync(ct);

        var dtos = await IncidentReader.ToDtosAsync(tenantUow, incidents, ct);
        return PagedResult<IncidentDto>.Ok(dtos.ToArray(), total, req.PageSize);
    }
}
