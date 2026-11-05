using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

internal sealed class DeleteVehiclePermitHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteVehiclePermitCommand, Result>
{
    public async Task<Result> Handle(DeleteVehiclePermitCommand req, CancellationToken ct)
    {
        var repository = tenantUow.Repository<VehiclePermit>();
        var permit = await repository.GetByIdAsync(req.Id, ct);
        if (permit is null)
        {
            return Result.Fail($"Could not find the vehicle paper '{req.Id}'.");
        }

        repository.Delete(permit);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
