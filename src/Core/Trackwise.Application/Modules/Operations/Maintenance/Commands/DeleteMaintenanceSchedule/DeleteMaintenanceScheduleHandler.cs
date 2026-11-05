using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal sealed class DeleteMaintenanceScheduleHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteMaintenanceScheduleCommand, Result>
{
    public async Task<Result> Handle(DeleteMaintenanceScheduleCommand req, CancellationToken ct)
    {
        var repository = tenantUow.Repository<MaintenanceSchedule>();
        var schedule = await repository.GetByIdAsync(req.Id, ct);
        if (schedule is null)
        {
            return Result.Fail($"Could not find maintenance schedule with ID '{req.Id}'");
        }

        repository.Delete(schedule);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
