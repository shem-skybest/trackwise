using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateBranchHandler(ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreateBranchCommand, Result<BranchDto>>
{
    public async Task<Result<BranchDto>> Handle(CreateBranchCommand req, CancellationToken ct)
    {
        var code = Branch.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = Branch.NormalizeCode(await codes.NextAsync(nameof(Branch), "BR", ct: ct));
        }
        else
        {
            // Soft-deleted branches keep their codes, and the unique index is filtered to live rows
            // only - so a clash with a deleted record is allowed, but the steward should be told
            // what they are reusing rather than silently colliding with history.
            var clash = await tenantUow.Repository<Branch>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(b => b.Code == code, ct);

            if (clash is not null)
            {
                return Result<BranchDto>.Fail(clash.DeletedAt is null
                    ? $"A branch with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted branch. Restore that branch or choose another code.");
            }
        }

        var branch = new Branch
        {
            Code = code,
            Name = req.Name,
            ShortName = req.ShortName,
            Address = req.Address,
            Phone = req.Phone,
            Email = req.Email,
            TaxId = req.TaxId
        };

        await tenantUow.Repository<Branch>().AddAsync(branch, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<BranchDto>.Ok(branch.ToDto());
    }
}
