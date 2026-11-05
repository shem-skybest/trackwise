using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Queries;

internal sealed class GetCrewPlanHandler(ITenantUnitOfWork tenantUow, ICrewPlanService crewPlan)
    : IAppRequestHandler<GetCrewPlanQuery, Result<IList<CrewPlanRowDto>>>
{
    public async Task<Result<IList<CrewPlanRowDto>>> Handle(GetCrewPlanQuery req, CancellationToken ct)
    {
        var date = req.Date ?? crewPlan.LocalDate(DateTime.UtcNow);

        var assignments = await tenantUow.Repository<CrewAssignment>().Query()
            .Where(a => a.Date == date)
            .ToDictionaryAsync(a => a.TruckId, ct);
        var plannedTruckIds = assignments.Keys.ToList();

        // A truck retired since it was planned still shows on that day, so a past plan reads as it was.
        var trucks = tenantUow.Repository<Truck>().Query()
            .Where(t => t.IsActive || plannedTruckIds.Contains(t.Id));
        if (req.TruckId is { } truckId)
        {
            trucks = trucks.Where(t => t.Id == truckId);
        }

        if (req.TruckType is { } type)
        {
            trucks = trucks.Where(t => t.Type == type);
        }

        if (req.BranchId is { } branchId)
        {
            trucks = trucks.Where(t => t.BranchId == branchId);
        }

        var truckList = await trucks.OrderBy(t => t.Number).ToListAsync(ct);
        var names = await LoadNamesAsync(truckList, assignments.Values, ct);

        var rows = truckList
            .Select(t => t.ToPlanRow(date, assignments.GetValueOrDefault(t.Id), names))
            .ToList();
        return Result<IList<CrewPlanRowDto>>.Ok(rows);
    }

    private async Task<Dictionary<Guid, string>> LoadNamesAsync(
        IEnumerable<Truck> trucks, IEnumerable<CrewAssignment> assignments, CancellationToken ct)
    {
        var plan = assignments.ToList();
        var employeeIds = trucks.Select(t => t.MainDriverId)
            .Concat(plan.Select(a => a.DriverId))
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var helperIds = plan.SelectMany(a => new[] { a.HelperId, a.SecondHelperId })
            .OfType<Guid>()
            .Distinct()
            .ToList();

        var employees = await tenantUow.Repository<Employee>().Query()
            .Where(e => employeeIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Name })
            .ToListAsync(ct);
        var helpers = await tenantUow.Repository<Helper>().Query()
            .Where(h => helperIds.Contains(h.Id))
            .Select(h => new { h.Id, h.Name })
            .ToListAsync(ct);

        return employees.Concat(helpers).ToDictionary(p => p.Id, p => p.Name);
    }
}
