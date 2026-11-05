using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services;

/// <summary>
/// Enforces the rule that every counterparty named on a workfile resolves to an active business
/// partner holding the matching role. Shared by create and update so the two cannot drift.
/// </summary>
internal static class WorkfilePartnerValidation
{
    public static async Task<Result> ValidateAsync(
        ITenantRepository<BusinessPartner, Guid> partnerRepo,
        WorkfileHeaderData header,
        CancellationToken ct)
    {
        (Guid? Id, BusinessPartnerRole Role, string Label)[] slots =
        [
            (header.ShippingLineId, BusinessPartnerRole.ShippingLine, "Shipping line"),
            (header.CoLoaderId, BusinessPartnerRole.CoLoader, "Co-loader"),
            (header.ForwarderId, BusinessPartnerRole.Forwarder, "Forwarder"),
            (header.TruckerId, BusinessPartnerRole.Trucker, "Trucker"),
            (header.BrokerId, BusinessPartnerRole.Broker, "Broker")
        ];

        var ids = slots.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Result.Ok();
        }

        // One round trip for all five slots; loading them one at a time would be an N+1 on a path
        // that runs on every header save.
        //
        // Read past the soft-delete filter on purpose. Without it a partner somebody deleted comes
        // back as "not a known business partner", which sends the clerk looking for a typo in a
        // record that is sitting right there - and would say the same thing about a genuine typo.
        // The refusal is the same either way; the sentence is not.
        var partners = await partnerRepo.Query()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        foreach (var (id, role, label) in slots)
        {
            if (!id.HasValue)
            {
                continue;
            }

            if (!partners.TryGetValue(id.Value, out var partner))
            {
                return Result.Fail($"{label} is not a known business partner.");
            }

            if (partner.MergedIntoId is not null)
            {
                return Result.Fail(
                    $"{label} '{partner.Name}' has been merged into another record. Pick the "
                    + "surviving partner instead.");
            }

            if (partner.DeletedAt is not null)
            {
                return Result.Fail($"{label} '{partner.Name}' has been deleted and cannot be used.");
            }

            if (!partner.CanActAs(role))
            {
                if (partner.ApprovalState is not MasterDataApprovalState.Approved)
                {
                    // A draft is not a company anybody has agreed exists yet. Named rather than
                    // folded into "inactive", because the two have different ways out: an inactive
                    // partner is reactivated by a steward, a draft needs a second person.
                    return Result.Fail(
                        $"{label} '{partner.Name}' has not been approved yet and cannot be used.");
                }

                return partner.IsActive
                    ? Result.Fail($"{label} '{partner.Name}' is not registered as a {label.ToLowerInvariant()}.")
                    : Result.Fail($"{label} '{partner.Name}' is inactive and cannot be used.");
            }
        }

        return Result.Ok();
    }
}
