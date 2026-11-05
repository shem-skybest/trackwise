using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

internal sealed class GetMaintenanceRecordByIdHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetMaintenanceRecordByIdQuery, Result<MaintenanceRecordDto>>
{
    public async Task<Result<MaintenanceRecordDto>> Handle(GetMaintenanceRecordByIdQuery req, CancellationToken ct)
    {
        var entity = await tenantUow.Repository<MaintenanceRecord>().GetByIdAsync(req.Id, ct);

        if (entity is null)
        {
            return Result<MaintenanceRecordDto>.Fail($"Could not find a maintenance record with ID '{req.Id}'");
        }

        var truckNumber = await TruckRefResolver.ResolveNumberAsync(tenantUow, entity.TruckId, ct);
        return Result<MaintenanceRecordDto>.Ok(entity.ToDto(truckNumber));
    }
}
