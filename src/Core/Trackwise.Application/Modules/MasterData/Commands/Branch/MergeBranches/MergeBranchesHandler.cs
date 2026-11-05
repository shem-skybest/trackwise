using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergeBranchesHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergeBranchesCommand, Result<BranchDto>>
{
    public Task<Result<BranchDto>> Handle(MergeBranchesCommand req, CancellationToken ct)
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
                var records = await tenantUow.Repository<Branch>().Query()
                    .IgnoreQueryFilters()
                    .Where(b => b.Id == req.SurvivorId || b.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(b => b.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(b => b.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<BranchDto>.Fail($"Could not find the surviving branch '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<BranchDto>.Fail($"Could not find the branch to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup could
                // then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<BranchDto>.Fail(
                        $"Branch {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<BranchDto>.Fail(
                        $"Branch {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                await merges.MergeAsync(
                    nameof(Branch), survivor, loser, req.FieldChoices, req.Reason, token);

                // A merged-away branch must not keep holding its code hostage - the survivor's code is the
                // one that resolves now, and freeing the loser's lets a future record reuse it.

                return Result<BranchDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        Branch survivor, Branch loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(Branch.Name): survivor.Name = loser.Name; break;
                case nameof(Branch.ShortName): survivor.ShortName = loser.ShortName; break;
                case nameof(Branch.Address): survivor.Address = loser.Address; break;
                case nameof(Branch.Phone): survivor.Phone = loser.Phone; break;
                case nameof(Branch.Email): survivor.Email = loser.Email; break;
                case nameof(Branch.TaxId): survivor.TaxId = loser.TaxId; break;

                // Code is never taken from the loser: it is the handle other systems join on, and
                // the loser's own code keeps resolving through the cross-reference instead.
            }
        }
    }
}
