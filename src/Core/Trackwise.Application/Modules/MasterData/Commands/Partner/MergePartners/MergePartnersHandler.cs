using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class MergePartnersHandler(
    ITenantUnitOfWork tenantUow,
    IMasterDataMergeService merges)
    : IAppRequestHandler<MergePartnersCommand, Result<BusinessPartnerDto>>
{
    public Task<Result<BusinessPartnerDto>> Handle(MergePartnersCommand req, CancellationToken ct)
    {
        // The whole handler is the transactional unit, not just the merge: the repoint is immediate
        // SQL while the survivor's edited fields are only staged, so a failure between them would
        // move live workfiles onto a record that was never written. Loading inside the delegate is
        // required too - a retry replays it against a cleared change tracker.
        return tenantUow.ExecuteInTransactionAsync(
            async token =>
            {
                // Both sides are read past the query filter: merging away a record a steward already
                // deleted is a normal cleanup, and blocking it would leave the duplicate stranded.
                var records = await tenantUow.Repository<BusinessPartner>().Query()
                    .IgnoreQueryFilters()
                    .Where(p => p.Id == req.SurvivorId || p.Id == req.LoserId)
                    .ToListAsync(token);

                var survivor = records.FirstOrDefault(p => p.Id == req.SurvivorId);
                var loser = records.FirstOrDefault(p => p.Id == req.LoserId);

                if (survivor is null)
                {
                    return Result<BusinessPartnerDto>.Fail(
                        $"Could not find the surviving partner '{req.SurvivorId}'.");
                }

                if (loser is null)
                {
                    return Result<BusinessPartnerDto>.Fail(
                        $"Could not find the partner to merge '{req.LoserId}'.");
                }

                // Merged is terminal. Re-merging a tombstone would fork the chain, and a later lookup
                // could then resolve to either end of it.
                if (loser.MergedIntoId is not null)
                {
                    return Result<BusinessPartnerDto>.Fail(
                        $"Partner {loser.Code} has already been merged. Merge the surviving record instead.");
                }

                if (survivor.MergedIntoId is not null)
                {
                    return Result<BusinessPartnerDto>.Fail(
                        $"Partner {survivor.Code} has itself been merged away and cannot be a survivor.");
                }

                // Two live records with different TINs are two legal entities, whatever they trade
                // as. Refused rather than resolved by a field choice, for the reason a vessel with a
                // conflicting IMO number is: the merge would destroy the one fact that makes this
                // list worth trusting, and it is not recoverable afterwards.
                if (survivor.TaxId is { } keep && loser.TaxId is { } drop && keep != drop)
                {
                    return Result<BusinessPartnerDto>.Fail(
                        $"These records carry different TINs ({keep} and {drop}), so they are different "
                        + "legal entities. Correct the wrong one before merging.");
                }

                // A merge is a steward's decision about which record survives, not a route around
                // approval: folding an approved partner into an unapproved one would publish the
                // draft under the survivor's references without anyone reviewing it.
                if (survivor.ApprovalState is not MasterDataApprovalState.Approved
                    && loser.ApprovalState is MasterDataApprovalState.Approved)
                {
                    return Result<BusinessPartnerDto>.Fail(
                        $"Partner {loser.Code} is approved and {survivor.Code} is not. Merge the other "
                        + "way round, or get the survivor approved first.");
                }

                ApplyFieldChoices(survivor, loser, req.FieldChoices);

                // Taking the loser's TIN is only safe once the loser has released it. The column is
                // indexed among live rows and the tombstone is not soft-deleted until the merge runs,
                // so clearing it here - inside the merge transaction - keeps the update from
                // colliding with a row that is about to stop counting.
                if (survivor.TaxId is null && loser.TaxId is { } inherited)
                {
                    loser.TaxId = null;
                    survivor.TaxId = inherited;
                }

                // Roles are unioned rather than chosen. Two records for one company usually exist
                // precisely because each office recorded the capacity it dealt with, so picking a
                // side would throw away half of what the survivor can now act as.
                survivor.Roles = [.. survivor.Roles.Union(loser.Roles)];

                await merges.MergeAsync(
                    nameof(BusinessPartner), survivor, loser, req.FieldChoices, req.Reason, token);

                return Result<BusinessPartnerDto>.Ok(survivor.ToDto());
            },
            ct);
    }

    /// <summary>
    /// Copies the fields the steward chose to take from the losing record. Entity-specific by
    /// design: the generic merge service handles forwarding, this stays type-safe.
    /// </summary>
    private static void ApplyFieldChoices(
        BusinessPartner survivor, BusinessPartner loser, IReadOnlyDictionary<string, string> choices)
    {
        foreach (var (field, side) in choices)
        {
            if (!string.Equals(side, "loser", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (field)
            {
                case nameof(BusinessPartner.Name): survivor.Name = loser.Name; break;
                case nameof(BusinessPartner.Address): survivor.Address = loser.Address; break;
                case nameof(BusinessPartner.ContactName): survivor.ContactName = loser.ContactName; break;
                case nameof(BusinessPartner.Email): survivor.Email = loser.Email; break;
                case nameof(BusinessPartner.Phone): survivor.Phone = loser.Phone; break;
                case nameof(BusinessPartner.CustomerId): survivor.CustomerId = loser.CustomerId; break;
                case nameof(BusinessPartner.Notes): survivor.Notes = loser.Notes; break;
                case nameof(BusinessPartner.BranchId): survivor.BranchId = loser.BranchId; break;

                // Code is never taken from the loser: it is the handle other systems join on, and
                // the loser's own code keeps resolving through the cross-reference instead.
                // TaxId is not a choice either - the guard above has already established that at
                // most one side has one, and the survivor inherits it automatically.
                // Roles are not a choice: they are unioned.
                // ApprovalState is not a choice at all. It is the record's own standing with an
                // approver, and handing it over is exactly the thing approval exists to prevent.
            }
        }
    }
}
