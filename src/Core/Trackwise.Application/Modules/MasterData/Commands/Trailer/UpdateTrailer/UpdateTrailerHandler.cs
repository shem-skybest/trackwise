using Trackwise.Application.Abstractions;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateTrailerHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<UpdateTrailerCommand, Result>
{
    public async Task<Result> Handle(UpdateTrailerCommand req, CancellationToken ct)
    {
        var trailer = await tenantUow.Repository<Trailer>().GetByIdAsync(req.Id, ct);
        if (trailer is null)
        {
            return Result.Fail($"Could not find trailer with ID '{req.Id}'");
        }

        var vin = Trailer.NormalizeVin(req.Vin);

        if (vin is not null && vin != trailer.Vin)
        {
            var holder = await tenantUow.Repository<Trailer>().Query()
                .FirstOrDefaultAsync(t => t.Vin == vin && t.Id != trailer.Id, ct);

            if (holder is not null)
            {
                return Result.Fail(
                    $"VIN {vin} already belongs to '{holder.Name}' ({holder.Code}). "
                    + "If these are the same trailer, merge the two records instead.");
            }
        }

        if (req.BranchId is { } branchId && branchId != trailer.BranchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        trailer.Name = req.Name;
        trailer.Type = req.Type;
        trailer.Vin = req.Vin;
        trailer.LicensePlate = req.LicensePlate;
        trailer.LicensePlateState = req.LicensePlateState;
        trailer.Make = req.Make;
        trailer.Model = req.Model;
        trailer.Year = req.Year;
        trailer.AxleCount = req.AxleCount;
        trailer.MaxLoadCapacityKg = req.MaxLoadCapacityKg;
        trailer.Notes = req.Notes;
        trailer.BranchId = req.BranchId;
        trailer.IsActive = req.IsActive;

        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
