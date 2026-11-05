using Microsoft.EntityFrameworkCore;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Queries;

internal sealed class GetCrewDayHandler(ITenantUnitOfWork tenantUow, ICrewPlanService crewPlan)
    : IAppRequestHandler<GetCrewDayQuery, Result<IList<CrewDayMemberDto>>>
{
    public async Task<Result<IList<CrewDayMemberDto>>> Handle(GetCrewDayQuery req, CancellationToken ct)
    {
        var date = req.Date ?? crewPlan.LocalDate(DateTime.UtcNow);

        var entries = await tenantUow.Repository<CrewStatusEntry>().Query()
            .Where(s => s.Date == date)
            .ToListAsync(ct);
        var statuses = entries.ToDictionary(s => s.PersonId);

        var plan = await tenantUow.Repository<CrewAssignment>().Query()
            .Where(a => a.Date == date)
            .Select(a => new { a.TruckId, a.DriverId, a.HelperId, a.SecondHelperId })
            .ToListAsync(ct);
        var truckIds = plan.Select(a => a.TruckId).ToList();
        var truckNumbers = await tenantUow.Repository<Truck>().Query()
            .Where(t => truckIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Number, ct);
        var onTruck = plan
            .SelectMany(a => new[] { a.DriverId, a.HelperId, a.SecondHelperId }.OfType<Guid>().Select(id => (id, a.TruckId)))
            .ToDictionary(p => p.id, p => p.TruckId);

        // Retired crew still show on a day they have a status or a truck, so a past day reads as it was.
        var involved = statuses.Keys.Concat(onTruck.Keys).ToList();
        var drivers = await tenantUow.Repository<Employee>().Query()
            .Where(e => (e.IsActive && e.Role != null && e.Role.Name == TenantRoles.Driver) || involved.Contains(e.Id))
            .Select(e => new { e.Id, e.Name, e.Code, e.EmploymentType, e.HomeUnit, e.IsActive })
            .ToListAsync(ct);
        var helpers = await tenantUow.Repository<Helper>().Query()
            .Where(h => h.IsActive || involved.Contains(h.Id))
            .Select(h => new { h.Id, h.Name, h.Code, h.EmploymentType, h.HomeUnit, h.IsActive })
            .ToListAsync(ct);

        var members = drivers
            .Select(d => Member(d.Id, CrewMemberKind.Driver, d.Name, d.Code, d.EmploymentType, d.HomeUnit, d.IsActive))
            .OrderBy(m => m.Name)
            .Concat(helpers
                .Select(h => Member(h.Id, CrewMemberKind.Helper, h.Name, h.Code, h.EmploymentType, h.HomeUnit, h.IsActive))
                .OrderBy(m => m.Name))
            .ToList();
        return Result<IList<CrewDayMemberDto>>.Ok(members);

        CrewDayMemberDto Member(
            Guid id, CrewMemberKind kind, string name, string code,
            EmploymentType? employment, CrewUnit? unit, bool isActive)
        {
            var status = statuses.GetValueOrDefault(id);
            var truckId = onTruck.TryGetValue(id, out var t) ? t : (Guid?)null;
            return new CrewDayMemberDto
            {
                PersonId = id,
                Kind = kind,
                Name = name,
                Code = code,
                EmploymentType = employment,
                HomeUnit = unit,
                IsActive = isActive,
                Status = status?.Status ?? CrewDayStatus.Available,
                Notes = status?.Notes,
                TruckId = truckId,
                TruckNumber = truckId is { } key ? truckNumbers.GetValueOrDefault(key) : null
            };
        }
    }
}
