using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations.CrewPlan.Services;
using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

internal sealed class SetCrewAssignmentHandler(
    ITenantUnitOfWork tenantUow,
    ITripCrewService crewService,
    ICrewPlanService crewPlan)
    : IAppRequestHandler<SetCrewAssignmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SetCrewAssignmentCommand req, CancellationToken ct)
    {
        if (await tenantUow.Repository<Truck>().GetByIdAsync(req.TruckId, ct) is null)
        {
            return Result<Guid>.Fail($"Could not find the truck '{req.TruckId}'.");
        }

        var crew = await crewService.ResolveAsync(req.DriverId, req.HelperId, req.SecondHelperId, ct);
        if (!crew.IsSuccess)
        {
            return Result<Guid>.Fail(crew.Error!);
        }

        var assignment = await crewPlan.FindAsync(req.TruckId, req.Date, ct);
        var isNew = assignment is null;
        assignment ??= CrewAssignment.Create(req.Date, req.TruckId);

        try
        {
            assignment.AssignCrew(crew.Value!.Driver, crew.Value.Helper, crew.Value.SecondHelper);
        }
        catch (InvalidOperationException ex)
        {
            return Result<Guid>.Fail(ex.Message);
        }

        assignment.UnitStatus = req.UnitStatus;
        assignment.Notes = req.Notes;

        if (await crewPlan.FindConflictAsync(assignment, ct) is { } conflict)
        {
            return Result<Guid>.Fail(conflict);
        }

        if (await crewPlan.FindUnavailableAsync(req.Date, assignment.GetCrewIds(), ct) is { } unavailable)
        {
            return Result<Guid>.Fail(unavailable);
        }

        if (isNew)
        {
            await tenantUow.Repository<CrewAssignment>().AddAsync(assignment, ct);
        }

        await tenantUow.SaveChangesAsync(ct);
        return Result<Guid>.Ok(assignment.Id);
    }
}
