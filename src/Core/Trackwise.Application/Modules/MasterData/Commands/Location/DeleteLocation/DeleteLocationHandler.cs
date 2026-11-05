using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteLocationHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteLocationCommand, Result>
{
    public async Task<Result> Handle(DeleteLocationCommand req, CancellationToken ct)
    {
        var location = await tenantUow.Repository<Location>().GetByIdAsync(req.Id, ct);
        if (location is null)
        {
            return Result.Fail($"Could not find location with ID '{req.Id}'");
        }

        // The zone-still-contains-locations check is the dependency probe's, not this handler's:
        // Location declares itself as its own dependent, so the probe counts the children and can
        // be overridden with a recorded reason. A hand-rolled check here would be an unconditional
        // refusal the steward has no way past.
        tenantUow.Repository<Location>().Delete(location);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
