using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

internal sealed class GetMaintenanceRequisitionHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetMaintenanceRequisitionQuery, Result<MaintenanceRequisitionDto>>
{
    public async Task<Result<MaintenanceRequisitionDto>> Handle(GetMaintenanceRequisitionQuery req, CancellationToken ct)
    {
        var number = req.RequisitionNumber.Trim().ToUpperInvariant();
        var records = await tenantUow.Repository<MaintenanceRecord>().Query()
            .Where(r => r.RequisitionNumber == number)
            .OrderBy(r => r.ServiceDate)
            .ToListAsync(ct);

        if (records.Count == 0)
        {
            return Result<MaintenanceRequisitionDto>.Fail($"No service records are paid through requisition '{number}'.");
        }

        var truckNumbers = await TruckRefResolver.ResolveAsync(
            tenantUow, records.Select(r => r.TruckId).Distinct().ToArray(), ct);

        return Result<MaintenanceRequisitionDto>.Ok(new MaintenanceRequisitionDto
        {
            RequisitionNumber = number,
            FirstServiceDate = records[0].ServiceDate,
            LastServiceDate = records[^1].ServiceDate,
            VendorNames = records
                .Select(r => r.VendorName)
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            LaborCost = records.Sum(r => r.LaborCost),
            PartsCost = records.Sum(r => r.PartsCost),
            TotalCost = records.Sum(r => r.TotalCost),
            Records = records
                .Select(r => r.ToDto(truckNumbers.TryGetValue(r.TruckId, out var truck) ? truck.Number : null))
                .ToList()
        });
    }
}
