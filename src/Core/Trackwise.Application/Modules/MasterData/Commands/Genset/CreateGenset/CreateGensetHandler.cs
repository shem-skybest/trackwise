using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateGensetHandler(ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateGensetCommand, Result<GensetDto>>
{
    public async Task<Result<GensetDto>> Handle(CreateGensetCommand req, CancellationToken ct)
    {
        var code = Genset.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = Genset.NormalizeCode(await codes.NextAsync(nameof(Genset), "GEN", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted genset is allowed, but the steward should be told what they are
            // reusing rather than silently colliding with history.
            var clash = await tenantUow.Repository<Genset>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(g => g.Code == code, ct);

            if (clash is not null)
            {
                return Result<GensetDto>.Fail(clash.DeletedAt is null
                    ? $"A genset with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted genset. Restore that genset or choose another code.");
            }
        }

        // The serial-number index is unique among live rows, so a collision here would surface as a
        // database error the importer cannot attribute to a row. Checked explicitly so the steward
        // is told which genset already holds it - the duplicate probe warns, this refuses.
        var serialNumber = Genset.NormalizeSerialNumber(req.SerialNumber);

        if (serialNumber is not null)
        {
            var holder = await tenantUow.Repository<Genset>().Query()
                .FirstOrDefaultAsync(g => g.SerialNumber == serialNumber, ct);

            if (holder is not null)
            {
                return Result<GensetDto>.Fail(
                    $"Serial number {serialNumber} already belongs to '{holder.Name}' ({holder.Code}). "
                    + "A serial number identifies one genset for life, so this is that genset.");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<GensetDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        var genset = new Genset
        {
            Code = code,
            Name = req.Name,
            Status = req.Status,
            SerialNumber = req.SerialNumber,
            Make = req.Make,
            Model = req.Model,
            Notes = req.Notes,
            BranchId = req.BranchId
        };

        await tenantUow.Repository<Genset>().AddAsync(genset, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<GensetDto>.Ok(genset.ToDto());
    }
}
