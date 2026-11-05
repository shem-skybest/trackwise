using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateGensetHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateGensetCommand, Result>
{
    public async Task<Result> Handle(UpdateGensetCommand req, CancellationToken ct)
    {
        var genset = await tenantUow.Repository<Genset>().GetByIdAsync(req.Id, ct);
        if (genset is null)
        {
            return Result.Fail($"Could not find genset with ID '{req.Id}'");
        }

        var serialNumber = Genset.NormalizeSerialNumber(req.SerialNumber);

        if (serialNumber is not null && serialNumber != genset.SerialNumber)
        {
            var holder = await tenantUow.Repository<Genset>().Query()
                .FirstOrDefaultAsync(g => g.SerialNumber == serialNumber && g.Id != genset.Id, ct);

            if (holder is not null)
            {
                return Result.Fail(
                    $"Serial number {serialNumber} already belongs to '{holder.Name}' ({holder.Code}). "
                    + "If these are the same genset, merge the two records instead.");
            }
        }

        if (req.BranchId is { } branchId && branchId != genset.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        genset.Name = req.Name;
        genset.Status = req.Status;
        genset.SerialNumber = req.SerialNumber;
        genset.Make = req.Make;
        genset.Model = req.Model;
        genset.Notes = req.Notes;
        genset.BranchId = req.BranchId;
        genset.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
