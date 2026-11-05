using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeTrailersHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeTrailersCommand, Result<TrailerDto>>
{
    public Task<Result<TrailerDto>> Handle(MergeTrailersCommand req, CancellationToken ct)
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
                var records = await tenantUow.Repository<Trailer>().Query()
                    .IgnoreQueryFilters()
                    .Where(t => t.Id == req.SurvivorId || t.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(t => t.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(t => t.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<TrailerDto>.Fail($"Could not find the surviving trailer '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<TrailerDto>.Fail($"Could not find the trailer to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup
                // could then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<TrailerDto>.Fail(
                        $"Trailer {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<TrailerDto>.Fail(
                        $"Trailer {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                // Two live records holding different VINs are, by definition, two trailers. Merging
                // them would destroy the one fact that makes this list trustworthy, so it is refused
                // rather than resolved by a field choice.
                if (survivor.Vin is { } keep && loser.Vin is { } drop && keep != drop)
                {
                    return Result<TrailerDto>.Fail(
                        $"These records carry different VINs ({keep} and {drop}), so they are "
                        + "different trailers. Correct the wrong one before merging.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                // Taking the loser's VIN is safe only once the loser has released it: the index is
                // unique among live rows, and the tombstone is not soft-deleted until the merge runs.
                // Clearing it here, inside the merge transaction, is what keeps the update from
                // colliding with a row that is about to stop counting.
                if (survivor.Vin is null && loser.Vin is { } inherited)
                {
                    loser.Vin = null;
                    survivor.Vin = inherited;
                }

                await merges.MergeAsync(nameof(Trailer), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<TrailerDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Trailer survivor, Trailer loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Trailer.Name): survivor.Name = loser.Name; break;
                case nameof(Trailer.Type): survivor.Type = loser.Type; break;
                case nameof(Trailer.LicensePlate): survivor.LicensePlate = loser.LicensePlate; break;
                case nameof(Trailer.LicensePlateState): survivor.LicensePlateState = loser.LicensePlateState; break;
                case nameof(Trailer.Make): survivor.Make = loser.Make; break;
                case nameof(Trailer.Model): survivor.Model = loser.Model; break;
                case nameof(Trailer.Year): survivor.Year = loser.Year; break;
                case nameof(Trailer.AxleCount): survivor.AxleCount = loser.AxleCount; break;
                case nameof(Trailer.MaxLoadCapacityKg): survivor.MaxLoadCapacityKg = loser.MaxLoadCapacityKg; break;
                case nameof(Trailer.Notes): survivor.Notes = loser.Notes; break;
                case nameof(Trailer.BranchId): survivor.BranchId = loser.BranchId; break;

                    // Code is never taken from the loser: it is the handle other systems join on, and
                    // the loser's own code keeps resolving through the cross-reference instead.
                    // Vin is not a choice either - the guard above has already established that at most
                    // one side has one, and the survivor inherits it automatically.
            }
        }
    }
}
