using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

internal sealed class GetMaintenanceRecordsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetMaintenanceRecordsQuery, PagedResult<MaintenanceRecordDto>>
{
    public async Task<PagedResult<MaintenanceRecordDto>> Handle(GetMaintenanceRecordsQuery req, CancellationToken ct)
    {
        var baseQuery = tenantUow.Repository<MaintenanceRecord>().Query();

        if (req.TruckId.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.TruckId == req.TruckId);
        }

        if (req.Type.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.MaintenanceType == req.Type);
        }

        if (req.VehicleSystem.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.VehicleSystem == req.VehicleSystem);
        }

        if (!string.IsNullOrWhiteSpace(req.RequisitionNumber))
        {
            var requisition = req.RequisitionNumber.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(r => r.RequisitionNumber == requisition);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var search = req.Search.Trim().ToLower();
            baseQuery = baseQuery.Where(r =>
                (r.RequisitionNumber != null && r.RequisitionNumber.ToLower().Contains(search)) ||
                (r.VendorName != null && r.VendorName.ToLower().Contains(search)) ||
                (r.Description != null && r.Description.ToLower().Contains(search)) ||
                r.Truck.Number.ToLower().Contains(search));
        }

        if (req.FromDate.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.ServiceDate >= req.FromDate);
        }

        if (req.ToDate.HasValue)
        {
            baseQuery = baseQuery.Where(r => r.ServiceDate <= req.ToDate);
        }

        baseQuery = baseQuery.OrderBy(req.OrderBy ?? "-ServiceDate");

        var totalItems = baseQuery.Count();
        baseQuery = baseQuery.ApplyPaging(req.Page, req.PageSize);

        var records = await baseQuery.ToArrayAsync(ct);

        var truckNumbers = await TruckRefResolver.ResolveAsync(
            tenantUow, records.Select(r => r.TruckId).Distinct().ToArray(), ct);

        var dtos = records
            .Select(r => r.ToDto(
                truckNumbers.TryGetValue(r.TruckId, out var truckRef) ? truckRef.Number : null))
            .ToArray();

        return PagedResult<MaintenanceRecordDto>.Ok(dtos, totalItems, req.PageSize);
    }
}
