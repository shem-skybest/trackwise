using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal sealed class UpdateMaintenanceRecordHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateMaintenanceRecordCommand, Result<MaintenanceRecordDto>>
{
    public async Task<Result<MaintenanceRecordDto>> Handle(UpdateMaintenanceRecordCommand req, CancellationToken ct)
    {
        var record = await tenantUow.Repository<MaintenanceRecord>().GetByIdAsync(req.Id, ct);
        if (record is null)
        {
            return Result<MaintenanceRecordDto>.Fail($"Could not find maintenance record with ID '{req.Id}'");
        }

        var applied = await MaintenanceRecordWriter.ApplyAsync(tenantUow, record, req, ct);
        if (!applied.IsSuccess)
        {
            return Result<MaintenanceRecordDto>.Fail(applied.Error!);
        }

        await tenantUow.SaveChangesAsync(ct);

        var truckNumber = await TruckRefResolver.ResolveNumberAsync(tenantUow, record.TruckId, ct);
        return Result<MaintenanceRecordDto>.Ok(record.ToDto(truckNumber));
    }
}
