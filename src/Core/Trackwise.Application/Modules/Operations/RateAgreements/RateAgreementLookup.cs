using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.RateAgreements;

/// <summary>
/// The agreement and diesel price in force on a day. Shared by trip pricing and booking estimates
/// so a booking is estimated on exactly what its trip will later be priced on.
/// </summary>
internal static class RateAgreementLookup
{
    public static Task<RateAgreement?> FindInForceAsync(
        ITenantUnitOfWork tenantUow, Guid customerId, DateOnly date, CancellationToken ct) =>
        tenantUow.Repository<RateAgreement>().Query()
            .Where(a => a.CustomerId == customerId
                && (a.Status == RateAgreementStatus.Active || a.Status == RateAgreementStatus.Superseded)
                && a.EffectiveFrom <= date
                && (a.EffectiveTo == null || a.EffectiveTo >= date))
            .OrderByDescending(a => a.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

    /// <summary>Diesel prices move every Tuesday; a figure older than this has probably been overtaken.</summary>
    private const int DieselPriceStaleAfterDays = 7;

    public static async Task<(decimal? Price, string? Warning)> DieselPriceOnAsync(
        ITenantUnitOfWork tenantUow, RateAgreement agreement, DateOnly date, CancellationToken ct)
    {
        var inForce = await tenantUow.Repository<DieselPrice>().Query()
            .Where(d => d.EffectiveFrom <= date)
            .OrderByDescending(d => d.EffectiveFrom)
            .Select(d => new { d.PricePerLitre, d.EffectiveFrom })
            .FirstOrDefaultAsync(ct);

        var stale = agreement.FuelMethod is FuelAdjustmentMethod.DieselTierSurcharge
            && inForce is not null
            && date.DayNumber - inForce.EffectiveFrom.DayNumber > DieselPriceStaleAfterDays;

        return (inForce?.PricePerLitre, stale
            ? $"The diesel price was last set {inForce!.EffectiveFrom:MMM d}, more than a week before {date:MMM d}. Set this week's price before billing."
            : null);
    }
}
