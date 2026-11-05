using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities.Maintenance;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Commands;

internal sealed class DeleteMaintenanceRecordHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteMaintenanceRecordCommand, Result>
{
    public async Task<Result> Handle(DeleteMaintenanceRecordCommand req, CancellationToken ct)
    {
        var repository = tenantUow.Repository<MaintenanceRecord>();
        var record = await repository.GetByIdAsync(req.Id, ct);
        if (record is null)
        {
            return Result.Fail($"Could not find maintenance record with ID '{req.Id}'");
        }

        repository.Delete(record);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
