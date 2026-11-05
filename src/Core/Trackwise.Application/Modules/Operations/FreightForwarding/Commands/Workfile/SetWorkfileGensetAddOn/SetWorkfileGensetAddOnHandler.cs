using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetWorkfileGensetAddOnHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<SetWorkfileGensetAddOnCommand, Result>
{
    public async Task<Result> Handle(SetWorkfileGensetAddOnCommand req, CancellationToken ct)
    {
        var workfile = await tenantUow.Repository<Workfile>().GetByIdAsync(req.Id, ct);
        if (workfile is null)
        {
            return Result.Fail($"Could not find workfile with ID '{req.Id}'");
        }

        if (req.GensetId.HasValue)
        {
            var genset = await tenantUow.Repository<Genset>().GetByIdAsync(req.GensetId.Value, ct);
            if (genset is null || genset.MergedIntoId is not null)
            {
                return Result.Fail("That genset unit is not a known, current record.");
            }

            if (!genset.IsActive)
            {
                return Result.Fail($"Genset '{genset.Name}' is inactive and cannot be assigned.");
            }
        }

        try
        {
            workfile.SetGensetAddOn(req.GensetId, req.BillingType, req.HourlyRate, req.FlatAmount, req.Notes);
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
