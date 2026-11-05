using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateCommodityHandler(
    ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateCommodityCommand, Result<CommodityDto>>
{
    public async Task<Result<CommodityDto>> Handle(CreateCommodityCommand req, CancellationToken ct)
    {
        var code = Commodity.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = Commodity.NormalizeCode(await codes.NextAsync(nameof(Commodity), "CMD", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted commodity is allowed, but the steward should be told what they are
            // reusing rather than silently colliding with history.
            var clash = await tenantUow.Repository<Commodity>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Code == code, ct);

            if (clash is not null)
            {
                return Result<CommodityDto>.Fail(clash.DeletedAt is null
                    ? $"A commodity with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted commodity. Restore that commodity or choose another code.");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<CommodityDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        var commodity = new Commodity
        {
            Code = code,
            Name = req.Name,
            HsCode = req.HsCode,
            Description = req.Description,
            IsHazardous = req.IsHazardous,

            // Cleared rather than carried when the commodity is not hazardous. A UN number on a
            // non-hazardous line is a leftover from a steward changing their mind mid-form, and it
            // would read later as a classification nobody made.
            UnNumber = req.IsHazardous ? req.UnNumber : null,

            RequiresTemperatureControl = req.RequiresTemperatureControl,
            Notes = req.Notes,
            BranchId = req.BranchId
        };

        await tenantUow.Repository<Commodity>().AddAsync(commodity, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<CommodityDto>.Ok(commodity.ToDto());
    }
}
