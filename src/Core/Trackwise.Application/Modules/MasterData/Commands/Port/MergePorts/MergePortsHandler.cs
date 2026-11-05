using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergePortsHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergePortsCommand, Result<TerminalDto>>
{
    public Task<Result<TerminalDto>> Handle(MergePortsCommand req, CancellationToken ct)
    {
        // The whole handler is the transactional unit, not just the merge: the repoint is
        // immediate SQL while the survivor's edited fields are only staged, so a failure
        // between them would move live references onto a record that was never written.
        // Loading inside the delegate is required too - a retry replays it against a
        // cleared change tracker.
        return tenantUow.ExecuteInTransactionAsync(
            async token =>
            {
                // Both sides are read past the query filter: merging away a record a steward already
                // deleted is a normal cleanup, and blocking it would leave the duplicate stranded.
                var records = await tenantUow.Repository<Terminal>().Query()
                    .IgnoreQueryFilters()
                    .Where(t => t.Id == req.SurvivorId || t.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(t => t.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(t => t.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<TerminalDto>.Fail($"Could not find the surviving port '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<TerminalDto>.Fail($"Could not find the port to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup could
                // then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<TerminalDto>.Fail(
                        $"Port {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<TerminalDto>.Fail(
                        $"Port {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                await merges.MergeAsync(
                    nameof(Terminal), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<TerminalDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Terminal survivor, Terminal loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Terminal.Name): survivor.Name = loser.Name; break;
                case nameof(Terminal.CountryCode): survivor.CountryCode = loser.CountryCode; break;
                case nameof(Terminal.Type): survivor.Type = loser.Type; break;
                case nameof(Terminal.Address): survivor.Address = loser.Address; break;
                case nameof(Terminal.Notes): survivor.Notes = loser.Notes; break;
                case nameof(Terminal.BranchId): survivor.BranchId = loser.BranchId; break;

                    // Code is never taken from the loser: it is the handle other systems join on, and
                    // the loser's own code keeps resolving through the cross-reference instead.
            }
        }
    }
}
