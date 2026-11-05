using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateTrailerHandler(ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateTrailerCommand, Result<TrailerDto>>
{
    public async Task<Result<TrailerDto>> Handle(CreateTrailerCommand req, CancellationToken ct)
    {
        var code = Trailer.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = Trailer.NormalizeCode(await codes.NextAsync(nameof(Trailer), "TRL", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted trailer is allowed, but the steward should be told what they are
            // reusing rather than silently colliding with history.
            var clash = await tenantUow.Repository<Trailer>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Code == code, ct);

            if (clash is not null)
            {
                return Result<TrailerDto>.Fail(clash.DeletedAt is null
                    ? $"A trailer with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted trailer. Restore that trailer or choose another code.");
            }
        }

        // The VIN index is unique among live rows, so a collision here would surface as a database
        // error the importer cannot attribute to a row. Checked explicitly so the steward is told
        // which trailer already holds it - the duplicate probe warns, this refuses.
        var vin = Trailer.NormalizeVin(req.Vin);

        if (vin is not null)
        {
            var holder = await tenantUow.Repository<Trailer>().Query()
                .FirstOrDefaultAsync(t => t.Vin == vin, ct);

            if (holder is not null)
            {
                return Result<TrailerDto>.Fail(
                    $"VIN {vin} already belongs to '{holder.Name}' ({holder.Code}). "
                    + "A VIN identifies one trailer for life, so this is that trailer.");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<TrailerDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        var trailer = new Trailer
        {
            Code = code,
            Name = req.Name,
            Type = req.Type,
            Vin = req.Vin,
            LicensePlate = req.LicensePlate,
            LicensePlateState = req.LicensePlateState,
            Make = req.Make,
            Model = req.Model,
            Year = req.Year,
            AxleCount = req.AxleCount,
            MaxLoadCapacityKg = req.MaxLoadCapacityKg,
            Notes = req.Notes,
            BranchId = req.BranchId
        };

        await tenantUow.Repository<Trailer>().AddAsync(trailer, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<TrailerDto>.Ok(trailer.ToDto());
    }
}
