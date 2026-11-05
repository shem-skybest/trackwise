using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

internal sealed class GetUpcomingMaintenanceHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetUpcomingMaintenanceQuery, Result<List<MaintenanceScheduleDto>>>
{
    public async Task<Result<List<MaintenanceScheduleDto>>> Handle(GetUpcomingMaintenanceQuery req, CancellationToken ct)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(req.DaysAhead);

        var baseQuery = tenantUow.Repository<MaintenanceSchedule>()
            .Query()
            .Where(s => s.IsActive);

        if (req.TruckId.HasValue)
        {
            baseQuery = baseQuery.Where(s => s.TruckId == req.TruckId);
        }

        if (req.IncludeOverdue)
        {
            baseQuery = baseQuery.Where(s =>
                s.NextDueDate <= cutoffDate ||
                (s.NextDueDate.HasValue && s.NextDueDate < DateTime.UtcNow));
        }
        else
        {
            baseQuery = baseQuery.Where(s =>
                s.NextDueDate.HasValue &&
                s.NextDueDate >= DateTime.UtcNow &&
                s.NextDueDate <= cutoffDate);
        }

        var entities = await baseQuery
            .OrderBy(s => s.NextDueDate)
            .ToListAsync(ct);

        var truckNumbers = await TruckRefResolver.ResolveAsync(
            tenantUow, entities.Select(s => s.TruckId).Distinct().ToArray(), ct);

        var schedules = entities
            .Select(s => s.ToDto(
                truckNumbers.TryGetValue(s.TruckId, out var truckRef) ? truckRef.Number : null,
                currentOdometer: null))
            .ToList();

        return Result<List<MaintenanceScheduleDto>>.Ok(schedules);
    }
}
