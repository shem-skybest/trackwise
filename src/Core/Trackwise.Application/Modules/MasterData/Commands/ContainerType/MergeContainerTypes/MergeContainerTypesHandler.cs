using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeContainerTypesHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeContainerTypesCommand, Result<ContainerTypeDto>>
{
    public Task<Result<ContainerTypeDto>> Handle(MergeContainerTypesCommand req, CancellationToken ct)
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
                var records = await tenantUow.Repository<ContainerType>().Query()
                    .IgnoreQueryFilters()
                    .Where(c => c.Id == req.SurvivorId || c.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(c => c.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(c => c.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<ContainerTypeDto>.Fail(
                        $"Could not find the surviving container type '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<ContainerTypeDto>.Fail(
                        $"Could not find the container type to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup could
                // then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<ContainerTypeDto>.Fail(
                        $"Container type {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<ContainerTypeDto>.Fail(
                        $"Container type {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                // Vessel's IMO rule, for the same reason: an ISO size-type code is defined by the standard,
                // so two records carrying different ones are two different boxes however alike they look.
                // A steward who genuinely wants them as one has to clear the wrong code first, which makes
                // the choice of vocabulary an explicit act rather than a side effect of a merge.
                if (survivor.IsoSizeTypeCode is { } survivorIso
                    && loser.IsoSizeTypeCode is { } loserIso
                    && survivorIso != loserIso)
                {
                    return Result<ContainerTypeDto>.Fail(
                        $"'{survivor.Name}' is {survivorIso} and '{loser.Name}' is {loserIso}. Those are "
                        + "different ISO types - clear one of the codes if they really are the same box.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                await merges.MergeAsync(
                    nameof(ContainerType), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<ContainerTypeDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        ContainerType survivor, ContainerType loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(ContainerType.Name): survivor.Name = loser.Name; break;
                case nameof(ContainerType.Category): survivor.Category = loser.Category; break;
                case nameof(ContainerType.Notes): survivor.Notes = loser.Notes; break;

                // Only reachable when the survivor had none - two different codes are refused
                // above - so this fills a gap rather than overwriting a standard code.
                case nameof(ContainerType.IsoSizeTypeCode):
                    survivor.IsoSizeTypeCode = loser.IsoSizeTypeCode;
                    break;

                // The box's dimensions move as one unit. Taking a length without its high-cube flag
                // and TEU factor produces a record the validator would have refused on the form,
                // and a merge is not the place to acquire a state a create cannot reach.
                case nameof(ContainerType.LengthFeet):
                    survivor.LengthFeet = loser.LengthFeet;
                    survivor.IsHighCube = loser.LengthFeet.HasValue && loser.IsHighCube;
                    survivor.TeuFactor = loser.LengthFeet.HasValue ? loser.TeuFactor : null;
                    break;

                    // Code is never taken from the loser: it is the handle other systems join on, and
                    // the loser's own code keeps resolving through the cross-reference instead.
            }
        }
    }
}
