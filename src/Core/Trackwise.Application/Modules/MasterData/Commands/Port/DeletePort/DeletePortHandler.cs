using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeletePortHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeletePortCommand, Result>
{
    public async Task<Result> Handle(DeletePortCommand req, CancellationToken ct)
    {
        var terminal = await tenantUow.Repository<Terminal>().GetByIdAsync(req.Id, ct);
        if (terminal is null)
        {
            return Result.Fail($"Could not find port with ID '{req.Id}'");
        }

        // What used to be a hand-rolled pair of checks over containers and loads is now the
        // dependency probe reading the policy's rules - and unlike those checks, it can be
        // overridden with a recorded reason instead of being an unconditional refusal.
        tenantUow.Repository<Terminal>().Delete(terminal);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
