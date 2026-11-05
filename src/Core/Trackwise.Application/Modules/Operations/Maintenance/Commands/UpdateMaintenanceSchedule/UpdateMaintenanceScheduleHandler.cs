using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal sealed class UpdateMaintenanceScheduleHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateMaintenanceScheduleCommand, Result<MaintenanceScheduleDto>>
{
    public async Task<Result<MaintenanceScheduleDto>> Handle(UpdateMaintenanceScheduleCommand req, CancellationToken ct)
    {
        var schedule = await tenantUow.Repository<MaintenanceSchedule>().GetByIdAsync(req.Id, ct);
        if (schedule is null)
        {
            return Result<MaintenanceScheduleDto>.Fail($"Could not find maintenance schedule with ID '{req.Id}'");
        }

        var result = req.IsActive && !schedule.IsActive
            ? await MaintenanceScheduleWriter.EnsureOnlyActiveAsync(tenantUow, schedule, ct)
            : Result.Ok();
        if (result.IsSuccess)
        {
            result = MaintenanceScheduleWriter.Apply(schedule, req);
        }

        if (!result.IsSuccess)
        {
            return Result<MaintenanceScheduleDto>.Fail(result.Error!);
        }

        schedule.IsActive = req.IsActive;
        await tenantUow.SaveChangesAsync(ct);

        var truckNumber = await TruckRefResolver.ResolveNumberAsync(tenantUow, schedule.TruckId, ct);
        return Result<MaintenanceScheduleDto>.Ok(schedule.ToDto(truckNumber, currentOdometer: null));
    }
}
