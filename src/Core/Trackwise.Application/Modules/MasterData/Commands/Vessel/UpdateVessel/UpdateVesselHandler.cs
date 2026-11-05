using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateVesselHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateVesselCommand, Result>
{
    public async Task<Result> Handle(UpdateVesselCommand req, CancellationToken ct)
    {
        var vessel = await tenantUow.Repository<Vessel>().GetByIdAsync(req.Id, ct);
        if (vessel is null)
        {
            return Result.Fail($"Could not find vessel with ID '{req.Id}'");
        }

        // The IMO index is unique among live rows, so a collision would otherwise surface as a
        // database error with no field attached to it. Checked here so the steward is told which
        // ship already holds the number - and told to merge, since one IMO means one hull.
        var imo = Vessel.NormalizeImoNumber(req.ImoNumber);

        if (imo is not null && imo != vessel.ImoNumber)
        {
            var holder = await tenantUow.Repository<Vessel>().Query()
                .FirstOrDefaultAsync(v => v.ImoNumber == imo && v.Id != vessel.Id, ct);

            if (holder is not null)
            {
                return Result.Fail(
                    $"IMO number {imo} already belongs to '{holder.Name}' ({holder.Code}). "
                    + "If these are the same ship, merge the two records instead.");
            }
        }

        if (req.BranchId is { } branchId && branchId != vessel.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        vessel.Name = req.Name;
        vessel.ImoNumber = req.ImoNumber;
        vessel.CallSign = req.CallSign;
        vessel.FlagCountryCode = req.FlagCountryCode?.ToUpperInvariant();
        vessel.Type = req.Type;
        vessel.TeuCapacity = req.TeuCapacity;
        vessel.Notes = req.Notes;
        vessel.BranchId = req.BranchId;
        vessel.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
