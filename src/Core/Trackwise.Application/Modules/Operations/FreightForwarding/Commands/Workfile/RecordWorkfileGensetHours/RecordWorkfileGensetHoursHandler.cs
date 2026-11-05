using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class RecordWorkfileGensetHoursHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<RecordWorkfileGensetHoursCommand, Result>
{
    public async Task<Result> Handle(RecordWorkfileGensetHoursCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.Id, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        try
        {
            workfile.RecordGensetHoursUsed(req.HoursUsed);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        tenantUow.Repository<Workfile>().Update(workfile);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
