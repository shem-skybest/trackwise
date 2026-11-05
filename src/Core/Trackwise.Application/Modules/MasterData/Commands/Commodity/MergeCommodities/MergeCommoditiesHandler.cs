using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeCommoditiesHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeCommoditiesCommand, Result<CommodityDto>>
{
    public Task<Result<CommodityDto>> Handle(MergeCommoditiesCommand req, CancellationToken ct)
    {
        // The whole handler is the transactional unit, not just the merge: the repoint is
        // immediate SQL, and the survivor's edited fields are staged, so a failure between
        // them would move live references onto a record that was never written. Loading
        // inside the delegate is required too - a retry replays it against a cleared tracker.
        return tenantUow.ExecuteInTransactionAsync(
            async token =>
            {
                // Both sides are read past the query filter: merging away a record a steward already
                // deleted is a normal cleanup, and blocking it would leave the duplicate stranded.
                var records = await tenantUow.Repository<Commodity>().Query()
                    .IgnoreQueryFilters()
                    .Where(c => c.Id == req.SurvivorId || c.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(c => c.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(c => c.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<CommodityDto>.Fail(
                        $"Could not find the surviving commodity '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<CommodityDto>.Fail(
                        $"Could not find the commodity to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup could
                // then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<CommodityDto>.Fail(
                        $"Commodity {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<CommodityDto>.Fail(
                        $"Commodity {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                await merges.MergeAsync(
                    nameof(Commodity), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<CommodityDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Commodity survivor, Commodity loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Commodity.Name): survivor.Name = loser.Name; break;
                case nameof(Commodity.HsCode): survivor.HsCode = loser.HsCode; break;
                case nameof(Commodity.Description): survivor.Description = loser.Description; break;
                case nameof(Commodity.Notes): survivor.Notes = loser.Notes; break;
                case nameof(Commodity.BranchId): survivor.BranchId = loser.BranchId; break;

                // Hazard classification moves as one unit. Taking IsHazardous without UnNumber - or
                // the reverse - produces a record the validator would have refused on the form, and
                // a merge is not the place to acquire a state a create cannot reach.
                case nameof(Commodity.IsHazardous):
                    survivor.IsHazardous = loser.IsHazardous;
                    survivor.UnNumber = loser.IsHazardous ? loser.UnNumber : null;
                    break;

                case nameof(Commodity.RequiresTemperatureControl):
                    survivor.RequiresTemperatureControl = loser.RequiresTemperatureControl;
                    break;

                // Code is never taken from the loser: it is the handle other systems join on, and
                // the loser's own code keeps resolving through the cross-reference instead.
            }
        }
    }
}
