using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateCommodityHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateCommodityCommand, Result>
{
    public async Task<Result> Handle(UpdateCommodityCommand req, CancellationToken ct)
    {
        var commodity = await tenantUow.Repository<Commodity>().GetByIdAsync(req.Id, ct);
        if (commodity is null)
        {
            return Result.Fail($"Could not find commodity with ID '{req.Id}'");
        }

        if (req.BranchId is { } branchId && branchId != commodity.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        commodity.Name = req.Name;
        commodity.HsCode = req.HsCode;
        commodity.Description = req.Description;
        commodity.IsHazardous = req.IsHazardous;

        // Cleared on the way out of hazardous, matching create. Leaving the old number behind would
        // read later as a classification somebody made rather than one they withdrew.
        commodity.UnNumber = req.IsHazardous ? req.UnNumber : null;

        commodity.RequiresTemperatureControl = req.RequiresTemperatureControl;
        commodity.Notes = req.Notes;
        commodity.BranchId = req.BranchId;
        commodity.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
