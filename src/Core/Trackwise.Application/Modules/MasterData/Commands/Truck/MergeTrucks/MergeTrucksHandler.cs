using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeTrucksHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeTrucksCommand, Result<TruckDto>>
{
    public Task<Result<TruckDto>> Handle(MergeTrucksCommand req, CancellationToken ct)
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
                var records = await tenantUow.Repository<Truck>().Query()
                    .IgnoreQueryFilters()
                    .Where(t => t.Id == req.SurvivorId || t.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(t => t.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(t => t.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<TruckDto>.Fail($"Could not find the surviving truck '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<TruckDto>.Fail($"Could not find the truck to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup
                // could then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<TruckDto>.Fail(
                        $"Truck {loser.Number} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<TruckDto>.Fail(
                        $"Truck {survivor.Number} has itself been merged away and cannot be a survivor.");
                }

                // Two live records holding different VINs are, by definition, two trucks. Merging
                // them would destroy the one fact that makes this list trustworthy, so it is refused
                // rather than resolved by a field choice.
                if (survivor.Vin is { } keep && loser.Vin is { } drop && keep != drop)
                {
                    return Result<TruckDto>.Fail(
                        $"These records carry different VINs ({keep} and {drop}), so they are "
                        + "different trucks. Correct the wrong one before merging.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                // Taking the loser's VIN is safe only once the loser has released it: the index is
                // unique among live rows, and the tombstone is not soft-deleted until the merge runs.
                if (survivor.Vin is null && loser.Vin is { } inherited)
                {
                    loser.Vin = null;
                    survivor.Vin = inherited;
                }

                await merges.MergeAsync(nameof(Truck), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<TruckDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe. Driver assignment
    /// is deliberately not a choice here - it is a current operational fact about the survivor, not
    /// a descriptive field either record's history can outvote.
    /// </summary>
    private static void ApplyFieldChoices(
        Truck survivor, Truck loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Truck.Name): survivor.Name = loser.Name; break;
                case nameof(Truck.Type): survivor.Type = loser.Type; break;
                case nameof(Truck.VehicleCapacity): survivor.VehicleCapacity = loser.VehicleCapacity; break;
                case nameof(Truck.Make): survivor.Make = loser.Make; break;
                case nameof(Truck.Model): survivor.Model = loser.Model; break;
                case nameof(Truck.Year): survivor.Year = loser.Year; break;
                case nameof(Truck.LicensePlate): survivor.LicensePlate = loser.LicensePlate; break;
                case nameof(Truck.LicensePlateState): survivor.LicensePlateState = loser.LicensePlateState; break;
                case nameof(Truck.BranchId): survivor.BranchId = loser.BranchId; break;

                    // Number is never taken from the loser: it is the handle other systems join on, and
                    // the loser's own number keeps resolving through the cross-reference instead.
                    // Vin is not a choice either - the guard above has already established that at most
                    // one side has one, and the survivor inherits it automatically.
            }
        }
    }
}
