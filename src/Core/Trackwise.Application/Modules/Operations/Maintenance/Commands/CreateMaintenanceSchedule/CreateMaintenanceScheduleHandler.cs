using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal sealed class CreateMaintenanceScheduleHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateMaintenanceScheduleCommand, Result<MaintenanceScheduleDto>>
{
    public async Task<Result<MaintenanceScheduleDto>> Handle(CreateMaintenanceScheduleCommand req, CancellationToken ct)
    {
        if (await tenantUow.Repository<Truck>().GetByIdAsync(req.TruckId, ct) is null)
        {
            return Result<MaintenanceScheduleDto>.Fail("Truck not found.");
        }

        var schedule = new MaintenanceSchedule
        {
            TruckId = req.TruckId,
            MaintenanceType = req.Type,
            IntervalType = req.IntervalType
        };

        var result = await MaintenanceScheduleWriter.EnsureOnlyActiveAsync(tenantUow, schedule, ct);
        if (result.IsSuccess)
        {
            result = MaintenanceScheduleWriter.Apply(schedule, req);
        }

        if (!result.IsSuccess)
        {
            return Result<MaintenanceScheduleDto>.Fail(result.Error!);
        }

        await tenantUow.Repository<MaintenanceSchedule>().AddAsync(schedule, ct);
        await tenantUow.SaveChangesAsync(ct);

        var truckNumber = await TruckRefResolver.ResolveNumberAsync(tenantUow, schedule.TruckId, ct);
        return Result<MaintenanceScheduleDto>.Ok(schedule.ToDto(truckNumber, currentOdometer: null));
    }
}
