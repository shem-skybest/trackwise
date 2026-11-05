using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeLocationsHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeLocationsCommand, Result<LocationDto>>
{
    public Task<Result<LocationDto>> Handle(MergeLocationsCommand req, CancellationToken ct)
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
                var records = await tenantUow.Repository<Location>().Query()
                    .IgnoreQueryFilters()
                    .Where(l => l.Id == req.SurvivorId || l.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(l => l.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(l => l.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<LocationDto>.Fail(
                        $"Could not find the surviving location '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<LocationDto>.Fail(
                        $"Could not find the location to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup could
                // then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<LocationDto>.Fail(
                        $"Location {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<LocationDto>.Fail(
                        $"Location {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                // A zone and a delivery point are not two records of the same thing, and folding one into
                // the other would either strand the zone's children or turn a street address into a
                // grouping. Refused rather than resolved by a field choice.
                if (survivor.Type != loser.Type)
                {
                    return Result<LocationDto>.Fail(
                        $"'{survivor.Name}' is a {survivor.Type} and '{loser.Name}' is a {loser.Type}. "
                        + "Only locations of the same kind can be merged.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                // The repoint that follows moves every child of the loser onto the survivor - including the
                // survivor itself, if it happened to sit inside the zone being folded in. That would leave
                // the survivor as its own parent, a one-node cycle no reader of the tree survives. Clearing
                // the link here means the save inside the merge transaction writes null over it.
                if (survivor.ParentLocationId == loser.Id)
                {
                    survivor.ParentLocationId = loser.ParentLocationId == survivor.Id
                        ? null
                        : loser.ParentLocationId;
                }

                await merges.MergeAsync(
                    nameof(Location), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<LocationDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Location survivor, Location loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Location.Name): survivor.Name = loser.Name; break;
                case nameof(Location.Address): survivor.Address = loser.Address; break;
                case nameof(Location.Notes): survivor.Notes = loser.Notes; break;
                case nameof(Location.BranchId): survivor.BranchId = loser.BranchId; break;
                case nameof(Location.CustomerId): survivor.CustomerId = loser.CustomerId; break;
                case nameof(Location.DefaultCutoffTime): survivor.DefaultCutoffTime = loser.DefaultCutoffTime; break;
                case nameof(Location.ZoneRadiusMeters): survivor.ZoneRadiusMeters = loser.ZoneRadiusMeters; break;

                // Coordinates move as a pair, for the same reason the validator requires both: half
                // a coordinate is a point in the Gulf of Guinea.
                case nameof(Location.Latitude):
                case nameof(Location.Longitude):
                    survivor.Latitude = loser.Latitude;
                    survivor.Longitude = loser.Longitude;
                    break;

                // Type is not a choice - the guard above has already established the two match.
                // Code is never taken from the loser: it is the handle other systems join on, and
                // the loser's own code keeps resolving through the cross-reference instead.
                // ParentLocationId is settled by the cycle guard in Handle, not here.
            }
        }
    }
}
