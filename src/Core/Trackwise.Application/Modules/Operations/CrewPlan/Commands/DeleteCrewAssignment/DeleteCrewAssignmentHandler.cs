using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

internal sealed class DeleteCrewAssignmentHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteCrewAssignmentCommand, Result>
{
    public async Task<Result> Handle(DeleteCrewAssignmentCommand req, CancellationToken ct)
    {
        var repository = tenantUow.Repository<CrewAssignment>();
        var assignment = await repository.GetByIdAsync(req.Id, ct);
        if (assignment is null)
        {
            return Result.Fail($"Could not find the crew assignment '{req.Id}'.");
        }

        repository.Delete(assignment);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
