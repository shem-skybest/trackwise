using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteTruckHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteTruckCommand, Result>
{
    public async Task<Result> Handle(DeleteTruckCommand req, CancellationToken ct)
    {
        var truck = await tenantUow.Repository<Truck>().GetByIdAsync(req.Id, ct);
        if (truck is null)
        {
            return Result.Fail($"Could not find truck with ID '{req.Id}'");
        }

        // What used to be a hand-rolled DeleteTenantEntityHandler is now the dependency probe
        // reading the policy's rules - and unlike a bare delete, it can be overridden with a
        // recorded reason instead of being an unconditional refusal.
        tenantUow.Repository<Truck>().Delete(truck);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
