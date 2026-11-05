using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateHelperHandler(ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateHelperCommand, Result<HelperDto>>
{
    public async Task<Result<HelperDto>> Handle(CreateHelperCommand req, CancellationToken ct)
    {
        var code = Helper.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = Helper.NormalizeCode(await codes.NextAsync(nameof(Helper), "HLP", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted helper is allowed, but the steward should be told what they are
            // reusing rather than silently colliding with history.
            var clash = await tenantUow.Repository<Helper>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(h => h.Code == code, ct);

            if (clash is not null)
            {
                return Result<HelperDto>.Fail(clash.DeletedAt is null
                    ? $"A helper with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted helper. Restore that helper or choose another code.");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<HelperDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        var helper = new Helper
        {
            Code = code,
            Name = req.Name,
            PhoneNumber = req.PhoneNumber,
            Address = req.Address,
            Notes = req.Notes,
            EmploymentType = req.EmploymentType,
            HomeUnit = req.HomeUnit,
            BranchId = req.BranchId
        };

        await tenantUow.Repository<Helper>().AddAsync(helper, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<HelperDto>.Ok(helper.ToDto());
    }
}
