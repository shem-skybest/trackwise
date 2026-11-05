using Trackwise.Application.Modules.Operations.Loads;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.Operations.RateAgreements;

/// <summary>A booking plan's agreement and sites, loaded and checked once for save and estimate.</summary>
internal sealed record BookingPlanContext(RateAgreement Agreement, IReadOnlyDictionary<Guid, Location> Sites);

/// <summary>
/// Prices a Cold Chain booking before its trip exists, with the same rules <see cref="TripRateCalculator"/>
/// applies to the trip: each drop is a saved site, and a distance band reads the straight-line distance
/// from the pickup warehouse, as a load's own distance does.
/// </summary>
internal static class BookingRateEstimator
{
    /// <summary>
    /// Loads the agreement in force on the service date and every site the plan names, refusing a site
    /// the client may not use.
    /// </summary>
    /// <param name="linkedSiteIds">Sites the booking already points at; a since-deactivated one may stay.</param>
    public static async Task<Result<BookingPlanContext>> PrepareAsync(
        ITenantUnitOfWork tenantUow,
        Guid customerId,
        ColdChainBookingPlan plan,
        IReadOnlySet<Guid> linkedSiteIds,
        CancellationToken ct)
    {
        var agreement = await RateAgreementLookup.FindInForceAsync(tenantUow, customerId, plan.ServiceDate, ct);
        if (agreement is null)
        {
            return Result<BookingPlanContext>.Fail(
                $"The client has no approved rate agreement in force on {plan.ServiceDate:yyyy-MM-dd}.");
        }

        var siteIds = plan.Drops.Select(d => d.SiteId)
            .Concat(plan.PickupSiteId is { } pickupId ? [pickupId] : [])
            .Distinct()
            .ToList();

        var sites = await tenantUow.Repository<Location>().Query()
            .Where(l => siteIds.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, ct);

        var missing = siteIds.Count(id => !sites.ContainsKey(id));
        if (missing > 0)
        {
            return Result<BookingPlanContext>.Fail($"{missing} of the chosen locations no longer exist.");
        }

        foreach (var site in sites.Values)
        {
            if (LoadSiteRules.Refusal(site, customerId, !linkedSiteIds.Contains(site.Id)) is { } refusal)
            {
                return Result<BookingPlanContext>.Fail(refusal);
            }
        }

        return Result<BookingPlanContext>.Ok(new BookingPlanContext(agreement, sites));
    }

    public static async Task<(TripRateQuote? Quote, BookingEstimateDto Dto)> EstimateAsync(
        ITenantUnitOfWork tenantUow, BookingPlanContext context, ColdChainBookingPlan plan, CancellationToken ct)
    {
        var agreement = context.Agreement;
        var (diesel, dieselWarning) = await RateAgreementLookup.DieselPriceOnAsync(tenantUow, agreement, plan.ServiceDate, ct);
        var pickup = plan.PickupSiteId is { } pickupId ? context.Sites[pickupId] : null;

        var drops = plan.Drops
            .Select(d => context.Sites[d.SiteId])
            .Select(site => new RatedDrop(
                site.Id,
                site.Id.ToString(),
                site.Address?.CityOrMunicipality,
                site.Address?.Province,
                DistanceKm(pickup, site)))
            .ToList();

        var dto = new BookingEstimateDto
        {
            AgreementId = agreement.Id,
            AgreementName = agreement.Name,
            DieselPrice = diesel,
            Currency = agreement.Currency
        };

        try
        {
            var quote = RatePricer.Price(agreement, plan.TruckSize, diesel, drops);

            dto.LineLabel = quote.LineLabel;
            dto.BaseAmount = quote.BaseAmount;
            dto.FuelAmount = quote.FuelAmount;
            dto.FuelBasis = quote.FuelBasis;
            dto.ExtraDrops = quote.ExtraDrops;
            dto.ExtraDropAmount = quote.ExtraDropAmount;
            dto.Total = quote.Total;
            dto.Warnings = [.. quote.Warnings, .. (dieselWarning is null ? [] : new[] { dieselWarning })];
            return (quote, dto);
        }
        catch (InvalidOperationException ex)
        {
            dto.Error = ex.Message;
            return (null, dto);
        }
    }

    /// <summary>Null unless both ends are pinned; a distance-band card then says it has no rate.</summary>
    private static double? DistanceKm(Location? from, Location to) =>
        from is { Latitude: { } fromLat, Longitude: { } fromLon } && to is { Latitude: { } toLat, Longitude: { } toLon }
            ? new GeoPoint(fromLon, fromLat).DistanceTo(new GeoPoint(toLon, toLat)) / 1000d
            : null;
}
