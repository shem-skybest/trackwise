using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal sealed class CreateMaintenanceRecordHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<CreateMaintenanceRecordCommand, Result<MaintenanceRecordDto>>
{
    public async Task<Result<MaintenanceRecordDto>> Handle(CreateMaintenanceRecordCommand req, CancellationToken ct)
    {
        var record = new MaintenanceRecord
        {
            TruckId = req.TruckId,
            MaintenanceType = req.Type,
            ServiceDate = req.ServiceDate
        };

        var applied = await MaintenanceRecordWriter.ApplyAsync(tenantUow, record, req, ct);
        if (!applied.IsSuccess)
        {
            return Result<MaintenanceRecordDto>.Fail(applied.Error!);
        }

        await tenantUow.Repository<MaintenanceRecord>().AddAsync(record, ct);
        await tenantUow.SaveChangesAsync(ct);

        var truckNumber = await TruckRefResolver.ResolveNumberAsync(tenantUow, record.TruckId, ct);
        return Result<MaintenanceRecordDto>.Ok(record.ToDto(truckNumber));
    }
}
