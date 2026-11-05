using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestoreTruckHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestoreTruckCommand, Result>
{
    public async Task<Result> Handle(RestoreTruckCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var truck = await tenantUow.Repository<Truck>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (truck is null)
        {
            return Result.Fail($"Could not find truck with ID '{req.Id}'");
        }

        if (truck.DeletedAt is null)
        {
            return Result.Fail("That truck is not deleted.");
        }

        // Numbers are unique among live rows only, so restoring can collide with a number that was
        // taken while this record was out of sight.
        var number = truck.Number;
        var holder = await tenantUow.Repository<Truck>().Query()
            .FirstOrDefaultAsync(t => t.Number == number && t.Id != truck.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Number '{number}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the number first.");
        }

        truck.DeletedAt = null;
        truck.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
