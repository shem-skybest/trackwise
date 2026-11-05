using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

internal sealed class SetCrewStatusHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SetCrewStatusCommand, Result<CrewStatusChangeDto>>
{
    public async Task<Result<CrewStatusChangeDto>> Handle(SetCrewStatusCommand req, CancellationToken ct)
    {
        var personId = req.PersonId;
        var isDriver = await tenantUow.Repository<Employee>().GetByIdAsync(personId, ct) is not null;
        if (!isDriver && await tenantUow.Repository<Helper>().GetByIdAsync(personId, ct) is null)
        {
            return Result<CrewStatusChangeDto>.Fail($"Could not find a driver or helper with ID '{personId}'.");
        }

        var from = req.Date;
        var until = req.UntilDate ?? req.Date;
        var repository = tenantUow.Repository<CrewStatusEntry>();
        var existing = await repository.Query()
            .Where(s => (s.EmployeeId == personId || s.HelperId == personId) && s.Date >= from && s.Date <= until)
            .ToDictionaryAsync(s => s.Date, ct);

        // Available with nothing to say is the default, so it is stored as no entry at all.
        var clears = req.Status == CrewDayStatus.Available && string.IsNullOrWhiteSpace(req.Notes);
        for (var day = from; day <= until; day = day.AddDays(1))
        {
            var entry = existing.GetValueOrDefault(day);
            if (clears)
            {
                if (entry is not null)
                {
                    repository.Delete(entry);
                }

                continue;
            }

            if (entry is null)
            {
                entry = isDriver ? CrewStatusEntry.ForDriver(day, personId) : CrewStatusEntry.ForHelper(day, personId);
                await repository.AddAsync(entry, ct);
            }

            entry.Status = req.Status;
            entry.Notes = req.Notes;
        }

        var removedFrom = req.Status.BlocksAssignment()
            ? await RemoveFromPlanAsync(personId, from, until, ct)
            : [];

        await tenantUow.SaveChangesAsync(ct);
        return Result<CrewStatusChangeDto>.Ok(new CrewStatusChangeDto
        {
            Days = until.DayNumber - from.DayNumber + 1,
            RemovedFrom = removedFrom
        });
    }

    /// <summary>Takes someone who isn't working off every truck they were planned on in the range.</summary>
    private async Task<List<string>> RemoveFromPlanAsync(Guid personId, DateOnly from, DateOnly until, CancellationToken ct)
    {
        var assignments = await tenantUow.Repository<CrewAssignment>().Query()
            .Where(a => a.Date >= from && a.Date <= until)
            .Where(a => a.DriverId == personId || a.HelperId == personId || a.SecondHelperId == personId)
            .OrderBy(a => a.Date)
            .ToListAsync(ct);
        if (assignments.Count == 0)
        {
            return [];
        }

        var truckIds = assignments.Select(a => a.TruckId).Distinct().ToList();
        var truckNumbers = await tenantUow.Repository<Truck>().Query()
            .Where(t => truckIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Number, ct);

        var removed = new List<string>();
        foreach (var assignment in assignments.Where(a => a.RemoveCrewMember(personId)))
        {
            removed.Add($"{truckNumbers.GetValueOrDefault(assignment.TruckId, "a truck")} on {assignment.Date:yyyy-MM-dd}");
        }

        return removed;
    }
}
