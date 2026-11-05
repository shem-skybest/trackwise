using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreatePortHandler(ITenantUnitOfWork tenantUow, IMasterDataCodeGenerator codes)
    : IAppRequestHandler<CreatePortCommand, Result<TerminalDto>>
{
    public async Task<Result<TerminalDto>> Handle(CreatePortCommand req, CancellationToken ct)
    {
        var code = Terminal.NormalizeCode(req.Code);

        if (code.Length == 0)
        {
            code = Terminal.NormalizeCode(await codes.NextAsync(nameof(Terminal), "PORT", ct: ct));
        }
        else
        {
            // Read past the query filter: the unique index is filtered to live rows, so a clash
            // with a deleted port is allowed, but the steward should be told what they are reusing
            // rather than silently colliding with history.
            var clash = await tenantUow.Repository<Terminal>().Query()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Code == code, ct);

            if (clash is not null)
            {
                return Result<TerminalDto>.Fail(clash.DeletedAt is null
                    ? $"A port with code '{code}' already exists"
                    : $"Code '{code}' belonged to a deleted port. Restore that port or choose another code.");
            }
        }

        if (req.BranchId is { } branchId)
        {
            var branch = await tenantUow.Repository<Branch>().GetByIdAsync(branchId, ct);

            if (branch is null)
            {
                return Result<TerminalDto>.Fail($"Could not find branch with ID '{branchId}'");
            }
        }

        var terminal = new Terminal
        {
            Code = code,
            Name = req.Name,
            CountryCode = req.CountryCode,
            Type = req.Type,
            Address = req.Address,
            Notes = req.Notes,
            BranchId = req.BranchId
        };

        await tenantUow.Repository<Terminal>().AddAsync(terminal, ct);
        await tenantUow.SaveChangesAsync(ct);
        return Result<TerminalDto>.Ok(terminal.ToDto());
    }
}
