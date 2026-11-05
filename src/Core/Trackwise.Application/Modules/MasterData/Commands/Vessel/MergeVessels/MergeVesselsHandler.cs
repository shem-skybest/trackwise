using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeVesselsHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeVesselsCommand, Result<VesselDto>>
{
    public Task<Result<VesselDto>> Handle(MergeVesselsCommand req, CancellationToken ct)
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
                var records = await tenantUow.Repository<Vessel>().Query()
                    .IgnoreQueryFilters()
                    .Where(v => v.Id == req.SurvivorId || v.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(v => v.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(v => v.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<VesselDto>.Fail($"Could not find the surviving vessel '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<VesselDto>.Fail($"Could not find the vessel to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup could
                // then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<VesselDto>.Fail(
                        $"Vessel {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<VesselDto>.Fail(
                        $"Vessel {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                // Two live records holding different IMO numbers are, by definition, two ships. Merging
                // them would destroy the one fact that makes this list trustworthy, so it is refused rather
                // than resolved by a field choice.
                if (survivor.ImoNumber is { } keep && loser.ImoNumber is { } drop && keep != drop)
                {
                    return Result<VesselDto>.Fail(
                        $"These records carry different IMO numbers ({keep} and {drop}), so they are "
                        + "different ships. Correct the wrong one before merging.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                // Taking the loser's IMO is safe only once the loser has released it: the index is unique
                // among live rows, and the tombstone is not soft-deleted until the merge runs. Clearing it
                // here, inside the merge transaction, is what keeps the update from colliding with a row
                // that is about to stop counting.
                if (survivor.ImoNumber is null && loser.ImoNumber is { } inherited)
                {
                    loser.ImoNumber = null;
                    survivor.ImoNumber = inherited;
                }

                await merges.MergeAsync(nameof(Vessel), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<VesselDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Vessel survivor, Vessel loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Vessel.Name): survivor.Name = loser.Name; break;
                case nameof(Vessel.CallSign): survivor.CallSign = loser.CallSign; break;
                case nameof(Vessel.FlagCountryCode): survivor.FlagCountryCode = loser.FlagCountryCode; break;
                case nameof(Vessel.Type): survivor.Type = loser.Type; break;
                case nameof(Vessel.TeuCapacity): survivor.TeuCapacity = loser.TeuCapacity; break;
                case nameof(Vessel.Notes): survivor.Notes = loser.Notes; break;
                case nameof(Vessel.BranchId): survivor.BranchId = loser.BranchId; break;

                // Code is never taken from the loser: it is the handle other systems join on, and
                // the loser's own code keeps resolving through the cross-reference instead.
                // ImoNumber is not a choice either - the guard above has already established that
                // at most one side has one, and the survivor inherits it automatically.
            }
        }
    }
}
