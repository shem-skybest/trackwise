using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeHelpersHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeHelpersCommand, Result<HelperDto>>
{
    public Task<Result<HelperDto>> Handle(MergeHelpersCommand req, CancellationToken ct)
    {
        // The whole handler is the transactional unit, not just the merge: the repoint is
        // immediate SQL while the survivor's edited fields are only staged, so a failure between
        // them would move live references onto a record that was never written. Loading inside the
        // delegate is required too - a retry replays it against a cleared change tracker.
        return tenantUow.ExecuteInTransactionAsync(
            async token =>
            {
                // Both sides are read past the query filter: merging away a record a steward already
                // deleted is a normal cleanup, and blocking it would leave the duplicate stranded.
                var records = await tenantUow.Repository<Helper>().Query()
                    .IgnoreQueryFilters()
                    .Where(h => h.Id == req.SurvivorId || h.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(h => h.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(h => h.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<HelperDto>.Fail($"Could not find the surviving helper '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<HelperDto>.Fail($"Could not find the helper to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup
                // could then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<HelperDto>.Fail(
                        $"Helper {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<HelperDto>.Fail(
                        $"Helper {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                await merges.MergeAsync(nameof(Helper), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<HelperDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Helper survivor, Helper loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Helper.Name): survivor.Name = loser.Name; break;
                case nameof(Helper.PhoneNumber): survivor.PhoneNumber = loser.PhoneNumber; break;
                case nameof(Helper.Address): survivor.Address = loser.Address; break;
                case nameof(Helper.Notes): survivor.Notes = loser.Notes; break;
                case nameof(Helper.BranchId): survivor.BranchId = loser.BranchId; break;

                    // Code is never taken from the loser: it is the handle other systems join on, and
                    // the loser's own code keeps resolving through the cross-reference instead.
            }
        }
    }
}
