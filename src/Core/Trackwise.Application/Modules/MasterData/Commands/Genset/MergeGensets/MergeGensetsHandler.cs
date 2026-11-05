using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeGensetsHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeGensetsCommand, Result<GensetDto>>
{
    public Task<Result<GensetDto>> Handle(MergeGensetsCommand req, CancellationToken ct)
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
                var records = await tenantUow.Repository<Genset>().Query()
                    .IgnoreQueryFilters()
                    .Where(g => g.Id == req.SurvivorId || g.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(g => g.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(g => g.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<GensetDto>.Fail($"Could not find the surviving genset '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<GensetDto>.Fail($"Could not find the genset to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup
                // could then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<GensetDto>.Fail(
                        $"Genset {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<GensetDto>.Fail(
                        $"Genset {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                // Two live records holding different serial numbers are, by definition, two gensets.
                // Merging them would destroy the one fact that makes this list trustworthy, so it is
                // refused rather than resolved by a field choice.
                if (survivor.SerialNumber is { } keep && loser.SerialNumber is { } drop && keep != drop)
                {
                    return Result<GensetDto>.Fail(
                        $"These records carry different serial numbers ({keep} and {drop}), so they "
                        + "are different gensets. Correct the wrong one before merging.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                // Taking the loser's serial number is safe only once the loser has released it: the
                // index is unique among live rows, and the tombstone is not soft-deleted until the
                // merge runs. Clearing it here, inside the merge transaction, is what keeps the
                // update from colliding with a row that is about to stop counting.
                if (survivor.SerialNumber is null && loser.SerialNumber is { } inherited)
                {
                    loser.SerialNumber = null;
                    survivor.SerialNumber = inherited;
                }

                await merges.MergeAsync(nameof(Genset), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<GensetDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Genset survivor, Genset loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Genset.Name): survivor.Name = loser.Name; break;
                case nameof(Genset.Status): survivor.Status = loser.Status; break;
                case nameof(Genset.Make): survivor.Make = loser.Make; break;
                case nameof(Genset.Model): survivor.Model = loser.Model; break;
                case nameof(Genset.Notes): survivor.Notes = loser.Notes; break;
                case nameof(Genset.BranchId): survivor.BranchId = loser.BranchId; break;

                    // Code is never taken from the loser: it is the handle other systems join on, and
                    // the loser's own code keeps resolving through the cross-reference instead.
                    // SerialNumber is not a choice either - the guard above has already established
                    // that at most one side has one, and the survivor inherits it automatically.
            }
        }
    }
}
