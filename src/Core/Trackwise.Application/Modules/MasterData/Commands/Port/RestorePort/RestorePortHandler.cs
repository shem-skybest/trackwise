using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class RestorePortHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RestorePortCommand, Result>
{
    public async Task<Result> Handle(RestorePortCommand req, CancellationToken ct)
    {
        // The global filter hides the very record this command exists to reach.
        var terminal = await tenantUow.Repository<Terminal>().Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == req.Id, ct);

        if (terminal is null)
        {
            return Result.Fail($"Could not find port with ID '{req.Id}'");
        }

        if (terminal.DeletedAt is null)
        {
            return Result.Fail("That port is not deleted.");
        }

        // Codes are unique among live rows only, so restoring can collide with a code that was
        // taken while this record was out of sight.
        var code = terminal.Code;
        var holder = await tenantUow.Repository<Terminal>().Query()
            .FirstOrDefaultAsync(t => t.Code == code && t.Id != terminal.Id, ct);

        if (holder is not null)
        {
            return Result.Fail(
                $"Code '{code}' is now used by '{holder.Name}'. Merge this record into that one "
                + "instead of restoring it, or free the code first.");
        }

        terminal.DeletedAt = null;
        terminal.DeletedBy = null;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
