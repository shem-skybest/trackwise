using Microsoft.EntityFrameworkCore;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Services;

internal sealed class CrewPlanService(ITenantUnitOfWork tenantUow) : ICrewPlanService
{
    public DateOnly LocalDate(DateTime utc) => tenantUow.GetCurrentTenant().Settings.LocalDate(utc);

    public Task<CrewAssignment?> FindAsync(Guid truckId, DateOnly date, CancellationToken ct = default) =>
        tenantUow.Repository<CrewAssignment>().Query()
            .FirstOrDefaultAsync(a => a.TruckId == truckId && a.Date == date, ct);

    public async Task<string?> FindConflictAsync(CrewAssignment assignment, CancellationToken ct = default)
    {
        var crewIds = assignment.GetCrewIds();
        if (crewIds.Count == 0)
        {
            return null;
        }

        var date = assignment.Date;
        var truckId = assignment.TruckId;
        var clash = await tenantUow.Repository<CrewAssignment>().Query()
            .Where(a => a.Date == date && a.TruckId != truckId)
            .Where(a => (a.DriverId != null && crewIds.Contains(a.DriverId.Value))
                        || (a.HelperId != null && crewIds.Contains(a.HelperId.Value))
                        || (a.SecondHelperId != null && crewIds.Contains(a.SecondHelperId.Value)))
            .Select(a => new { a.TruckId, a.DriverId, a.HelperId, a.SecondHelperId })
            .FirstOrDefaultAsync(ct);

        if (clash is null)
        {
            return null;
        }

        var personId = new[] { clash.DriverId, clash.HelperId, clash.SecondHelperId }
            .OfType<Guid>()
            .First(crewIds.Contains);
        var person = await NameOfAsync(personId, ct);
        var truck = await tenantUow.Repository<Truck>().GetByIdAsync(clash.TruckId, ct);
        return $"{person} is already on {truck?.Number ?? "another truck"}'s crew on {date:yyyy-MM-dd}.";
    }

    public async Task<string?> FindUnavailableAsync(
        DateOnly date, IReadOnlyCollection<Guid> personIds, CancellationToken ct = default)
    {
        if (personIds.Count == 0)
        {
            return null;
        }

        var ids = personIds.ToList();
        var blocked = await tenantUow.Repository<CrewStatusEntry>().Query()
            .Where(s => s.Date == date)
            .Where(s => (s.EmployeeId != null && ids.Contains(s.EmployeeId.Value))
                        || (s.HelperId != null && ids.Contains(s.HelperId.Value)))
            .Where(s => CrewDayStatusExtensions.Blocking.Contains(s.Status))
            .Select(s => new { s.EmployeeId, s.HelperId, s.Status })
            .FirstOrDefaultAsync(ct);

        if (blocked is null)
        {
            return null;
        }

        var person = await NameOfAsync(blocked.EmployeeId ?? blocked.HelperId!.Value, ct);
        var status = blocked.Status switch
        {
            CrewDayStatus.OnLeave => "on leave",
            CrewDayStatus.Suspended => "suspended",
            _ => "absent"
        };
        return $"{person} is {status} on {date:yyyy-MM-dd}.";
    }

    public DateOnly TripDay(IEnumerable<Load> loads)
    {
        var runsAt = loads.Select(l => l.RequestedPickupDate).OfType<DateTime>().DefaultIfEmpty(DateTime.UtcNow).Min();
        return LocalDate(runsAt);
    }

    private async Task<string> NameOfAsync(Guid personId, CancellationToken ct)
    {
        var employee = await tenantUow.Repository<Employee>().GetByIdAsync(personId, ct);
        if (employee is not null)
        {
            return employee.GetFullName();
        }

        var helper = await tenantUow.Repository<Helper>().GetByIdAsync(personId, ct);
        return helper?.Name ?? "Someone on this crew";
    }
}
