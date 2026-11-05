using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateVesselHandler(ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateVesselCommand, Result<VesselDto>>
{
    public async Task<Result<VesselDto>> Handle(CreateVesselCommand req, CancellationToken ct)
    {
        var code = Vessel.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = Vessel.NormalizeCode(await codes.NextAsync(nameof(Vessel), "VSL", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted vessel is allowed, but the steward should be told what they are
            // reusing rather than silently colliding with history.
            var clash = await tenantUow.Repository<Vessel>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.Code == code, ct);

            if (clash is not null)
            {
                return Result<VesselDto>.Fail(clash.DeletedAt is null
                    ? $"A vessel with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted vessel. Restore that vessel or choose another code.");
            }
        }

        // The IMO index is unique among live rows, so a collision here would surface as a database
        // error the importer cannot attribute to a row. Checked explicitly so the steward is told
        // which ship already holds the number - the duplicate probe warns, this refuses.
        var imo = Vessel.NormalizeImoNumber(req.ImoNumber);

        if (imo is not null)
        {
            var holder = await tenantUow.Repository<Vessel>().Query()
                .FirstOrDefaultAsync(v => v.ImoNumber == imo, ct);

            if (holder is not null)
            {
                return Result<VesselDto>.Fail(
                    $"IMO number {imo} already belongs to '{holder.Name}' ({holder.Code}). "
                    + "An IMO number identifies one ship for life, so this is that ship.");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<VesselDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        var vessel = new Vessel
        {
            Code = code,
            Name = req.Name,
            ImoNumber = req.ImoNumber,
            CallSign = req.CallSign,
            FlagCountryCode = req.FlagCountryCode?.ToUpperInvariant(),
            Type = req.Type,
            TeuCapacity = req.TeuCapacity,
            Notes = req.Notes,
            BranchId = req.BranchId
        };

        await tenantUow.Repository<Vessel>().AddAsync(vessel, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<VesselDto>.Ok(vessel.ToDto());
    }
}
