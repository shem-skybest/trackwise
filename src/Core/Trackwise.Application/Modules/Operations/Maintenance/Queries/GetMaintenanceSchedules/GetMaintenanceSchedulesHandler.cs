using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

internal sealed class GetMaintenanceSchedulesHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetMaintenanceSchedulesQuery, Result<List<MaintenanceScheduleDto>>>
{
    public async Task<Result<List<MaintenanceScheduleDto>>> Handle(GetMaintenanceSchedulesQuery req, CancellationToken ct)
    {
        var query = tenantUow.Repository<MaintenanceSchedule>().Query();
        if (req.TruckId.HasValue)
        {
            query = query.Where(s => s.TruckId == req.TruckId);
        }

        if (!req.IncludeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var schedules = await query
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.NextDueDate)
            .ToListAsync(ct);

        var truckNumbers = await TruckRefResolver.ResolveAsync(
            tenantUow, schedules.Select(s => s.TruckId).Distinct().ToArray(), ct);

        return Result<List<MaintenanceScheduleDto>>.Ok(schedules
            .Select(s => s.ToDto(
                truckNumbers.TryGetValue(s.TruckId, out var truck) ? truck.Number : null,
                currentOdometer: null))
            .ToList());
    }
}
