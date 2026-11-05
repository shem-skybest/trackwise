using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements;

/// <summary>
/// Gathers what <see cref="RatePricer"/> needs for one trip - the client's agreement and the diesel
/// price in force on the trip date, the size it's billed at, the drops - so previewing and applying a
/// price read exactly the same inputs.
/// </summary>
internal static class TripRateCalculator
{
    public static async Task<Result<(TripRateQuote Quote, DateOnly RatedOn, TripRateQuoteDto Dto)>> QuoteAsync(
        ITenantUnitOfWork tenantUow, Trip trip, DateOnly? ratedOn, CancellationToken ct)
    {
        var dropLoads = trip.Stops
            .Where(s => s.Type == TripStopType.DropOff)
            .OrderBy(s => s.Order)
            .Select(s => s.Load)
            .ToList();

        if (dropLoads.Count == 0)
        {
            return Fail("The trip has no drops to price.");
        }

        var customerIds = dropLoads.Select(l => l.CustomerId).Distinct().ToList();
        if (customerIds.Count > 1)
        {
            return Fail("The trip carries loads for more than one customer; a rate agreement prices one client's truck-trip.");
        }

        var date = ratedOn ?? TripDate(trip, dropLoads);
        var customerId = customerIds[0];

        var agreement = await RateAgreementLookup.FindInForceAsync(tenantUow, customerId, date, ct);

        if (agreement is null)
        {
            return Fail($"The customer has no approved rate agreement in force on {date:yyyy-MM-dd}.");
        }

        var (diesel, dieselWarning) = await RateAgreementLookup.DieselPriceOnAsync(tenantUow, agreement, date, ct);

        var drops = dropLoads
            .Select(l => new RatedDrop(
                l.Id,
                l.DestinationSiteId?.ToString() ?? PlaceKey(l.DestinationAddress.StreetAddress, l.DestinationAddress.CityOrMunicipality),
                l.DestinationAddress.CityOrMunicipality,
                l.DestinationAddress.Province,
                l.Distance > 0 ? l.Distance / 1000d : null,
                l.Status is LoadStatus.FoulTrip or LoadStatus.CancelledByClient))
            .ToList();

        try
        {
            var quote = RatePricer.Price(agreement, trip.PricingSize, diesel, drops);

            var dto = new TripRateQuoteDto
            {
                TripId = trip.Id,
                AgreementId = quote.AgreementId,
                AgreementName = quote.AgreementName,
                RatedOn = date,
                DieselPrice = diesel,
                LineLabel = quote.LineLabel,
                BaseAmount = quote.BaseAmount,
                FuelAmount = quote.FuelAmount,
                FuelBasis = quote.FuelBasis,
                ExtraDrops = quote.ExtraDrops,
                ExtraDropAmount = quote.ExtraDropAmount,
                FoulTripPercent = quote.FoulTripPercent,
                Total = quote.Total,
                Currency = agreement.Currency,
                Warnings = [.. quote.Warnings, .. SizeWarning(trip), .. (dieselWarning is null ? [] : new[] { dieselWarning })]
            };

            return Result<(TripRateQuote, DateOnly, TripRateQuoteDto)>.Ok((quote, date, dto));
        }
        catch (InvalidOperationException ex)
        {
            return Fail(ex.Message);
        }
    }

    private static IEnumerable<string> SizeWarning(Trip trip)
    {
        if (trip.BookedTruckSize is { } booked && trip.Truck?.TonnageClass is { } actual && actual != booked)
        {
            yield return $"Billed as the booked {RatePricer.SizeName(booked)}; the trip runs on a {RatePricer.SizeName(actual)}.";
        }
    }

    /// <summary>
    /// The day the trip runs, for picking the agreement and diesel price. Dates here are UTC, so the
    /// client passes its local date whenever it has one; this fallback only serves API callers.
    /// </summary>
    private static DateOnly TripDate(Trip trip, IReadOnlyList<Load> dropLoads)
    {
        var when = dropLoads.Select(l => l.RequestedPickupDate ?? l.RequestedDeliveryDate).FirstOrDefault(d => d.HasValue)
            ?? trip.GarageDepartedAt
            ?? trip.DispatchedAt
            ?? DateTime.UtcNow;

        return DateOnly.FromDateTime(when);
    }

    private static string PlaceKey(string street, string city) =>
        $"{street.Trim().ToLowerInvariant()}|{city.Trim().ToLowerInvariant()}";

    private static Result<(TripRateQuote, DateOnly, TripRateQuoteDto)> Fail(string message) =>
        Result<(TripRateQuote, DateOnly, TripRateQuoteDto)>.Fail(message);
}
