using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

internal sealed class CopyCrewPlanHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CopyCrewPlanCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CopyCrewPlanCommand req, CancellationToken ct)
    {
        var repository = tenantUow.Repository<CrewAssignment>();
        var source = await repository.Query().Where(a => a.Date == req.FromDate).ToListAsync(ct);
        var target = await repository.Query().Where(a => a.Date == req.ToDate).ToListAsync(ct);

        var activeTruckIds = await tenantUow.Repository<Truck>().Query()
            .Where(t => t.IsActive)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var plannedTrucks = target.Select(a => a.TruckId).ToHashSet();
        var toCopy = source
            .Where(a => activeTruckIds.Contains(a.TruckId) && !plannedTrucks.Contains(a.TruckId))
            .ToList();
        if (toCopy.Count == 0)
        {
            return Result<int>.Ok(0);
        }

        var crewIds = toCopy.SelectMany(a => a.GetCrewIds()).ToList();
        var drivers = await tenantUow.Repository<Employee>().Query()
            .Where(e => crewIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);
        var helpers = await tenantUow.Repository<Helper>().Query()
            .Where(h => crewIds.Contains(h.Id))
            .ToDictionaryAsync(h => h.Id, ct);

        // Anyone already on a truck that day, or not working it, stays off the copied rows.
        var busy = target.SelectMany(a => a.GetCrewIds()).ToHashSet();
        var notWorking = await tenantUow.Repository<CrewStatusEntry>().Query()
            .Where(s => s.Date == req.ToDate && CrewDayStatusExtensions.Blocking.Contains(s.Status))
            .Select(s => s.EmployeeId ?? s.HelperId!.Value)
            .ToListAsync(ct);
        busy.UnionWith(notWorking);

        foreach (var from in toCopy)
        {
            var copy = CrewAssignment.Create(req.ToDate, from.TruckId);
            copy.UnitStatus = from.UnitStatus;
            copy.Notes = from.Notes;
            copy.AssignCrew(
                Free(drivers, from.DriverId),
                Free(helpers, from.HelperId),
                Free(helpers, from.SecondHelperId));

            busy.UnionWith(copy.GetCrewIds());
            await repository.AddAsync(copy, ct);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result<int>.Ok(toCopy.Count);

        T? Free<T>(Dictionary<Guid, T> people, Guid? id) where T : class =>
            id is { } personId && !busy.Contains(personId) ? people.GetValueOrDefault(personId) : null;
    }
}
