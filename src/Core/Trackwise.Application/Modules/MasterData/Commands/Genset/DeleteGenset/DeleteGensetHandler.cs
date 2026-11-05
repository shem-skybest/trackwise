using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteGensetHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteGensetCommand, Result>
{
    public async Task<Result> Handle(DeleteGensetCommand req, CancellationToken ct)
    {
        var genset = await tenantUow.Repository<Genset>().GetByIdAsync(req.Id, ct);
        if (genset is null)
        {
            return Result.Fail($"Could not find genset with ID '{req.Id}'");
        }

        // No hand-rolled dependency check, and none needed: the probe runs in the pipeline off the
        // entity's declared rules. Genset declares none yet - nothing holds a genset by foreign key
        // until the Workfile-side attachment references it.
        tenantUow.Repository<Genset>().Delete(genset);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
