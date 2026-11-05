using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums.Safety;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

internal sealed class SetIncidentRepairsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SetIncidentRepairsCommand, Result<IncidentDto>>
{
    public Task<Result<IncidentDto>> Handle(SetIncidentRepairsCommand req, CancellationToken ct) =>
        IncidentEdit.ApplyAsync(tenantUow, req.Id, async incident =>
        {
            if (incident.Status == IncidentStatus.Closed)
            {
                return Result.Fail("This incident is closed and can't be changed.");
            }

            var wanted = req.RepairIds.Distinct().ToList();
            var records = await tenantUow.Repository<MaintenanceRecord>().Query()
                .Where(r => wanted.Contains(r.Id) || r.IncidentId == incident.Id)
                .ToListAsync(ct);

            if (wanted.Except(records.Select(r => r.Id)).Any())
            {
                return Result.Fail("One of the repairs no longer exists.");
            }

            foreach (var record in records.Where(r => wanted.Contains(r.Id)))
            {
                if (record.IncidentId is { } other && other != incident.Id)
                {
                    return Result.Fail($"The repair of {record.ServiceDate:d MMM yyyy} is already linked to another incident.");
                }

                if (incident.TruckId is { } truckId && record.TruckId != truckId)
                {
                    return Result.Fail($"The repair of {record.ServiceDate:d MMM yyyy} is on another truck.");
                }
            }

            foreach (var record in records)
            {
                record.IncidentId = wanted.Contains(record.Id) ? incident.Id : null;
            }

            return null;
        }, ct);
}
