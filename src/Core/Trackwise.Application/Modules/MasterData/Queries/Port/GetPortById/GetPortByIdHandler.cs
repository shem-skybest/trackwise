using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

/// <summary>
/// Reads a port by id, following the merge chain when the id belongs to a record that has since
/// been folded into another.
/// <para>
/// This is what makes merging safe: an old load holding a merged id still resolves, to the
/// survivor, flagged as forwarded rather than silently substituted.
/// </para>
/// </summary>
internal sealed class GetPortByIdHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<GetPortByIdQuery, Result<TerminalDto>>
{
    public async Task<Result<TerminalDto>> Handle(GetPortByIdQuery req, CancellationToken ct)
    {
        var terminal = await tenantUow.Repository<Terminal>().GetByIdAsync(req.Id, ct);

        if (terminal is not null)
        {
            return Result<TerminalDto>.Ok(await ToDtoAsync(terminal, ct));
        }

        // Not live under that id. Either it never existed, or it was merged away - the chain says
        // which.
        var (resolvedId, forwarded) = await merges.ResolveAsync(nameof(Terminal), req.Id, ct);

        if (!forwarded)
        {
            return Result<TerminalDto>.Fail($"Could not find port with ID '{req.Id}'");
        }

        var survivor = await tenantUow.Repository<Terminal>().GetByIdAsync(resolvedId, ct);

        if (survivor is null)
        {
            return Result<TerminalDto>.Fail($"Could not find port with ID '{req.Id}'");
        }

        var dto = await ToDtoAsync(survivor, ct);

        // Telling the caller their reference was superseded matters: a UI that silently swapped the
        // record would leave a steward editing something they did not open.
        dto.ForwardedFromId = req.Id;

        return Result<TerminalDto>.Ok(dto);
    }

    private async Task<TerminalDto> ToDtoAsync(Terminal terminal, CancellationToken ct) =>
        terminal.ToDto(await BranchNameLookup.ResolveAsync(tenantUow, [terminal.BranchId], ct));
}
