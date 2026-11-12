using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Services.BookingRequests;

/// <summary>
/// Turns the model's JSON into proposals, resolving site keys against the list the model was shown.
/// A key the model made up is dropped with a warning, so a proposal can only ever point at one of
/// the client's own locations or a shared site.
/// </summary>
internal static class BookingRequestReplyParser
{
    private const int MaxCandidates = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static Result<BookingRequestReadingDto> Parse(
        string llmText, IReadOnlyDictionary<string, ProposedSiteDto> sitesByKey)
    {
        var reply = Deserialize(llmText);
        if (reply is null)
        {
            return Result<BookingRequestReadingDto>.Fail(
                "The AI's answer could not be read. Try again, or type the booking in by hand.");
        }

        var warnings = reply.Warnings?.Where(w => !string.IsNullOrWhiteSpace(w)).ToList() ?? [];
        var sitesByName = sitesByKey.Values.ToLookup(s => NormalizeName(s.Name));

        ProposedSiteDto? Resolve(string? key, string? text)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;
            if (sitesByKey.TryGetValue(key.Trim(), out var site))
                return site;

            warnings.Add($"\"{text ?? key}\" was matched to a location that isn't on the client's list; pick it by hand.");
            return null;
        }

        var trips = (reply.Trips ?? []).Select(trip => new ProposedColdChainTripDto
        {
            ServiceDate = ParseDate(trip.ServiceDate),
            CallTime = ParseTime(trip.CallTime),
            TruckSize = ParseTruckSize(trip.TruckSize),
            PickupSite = Resolve(trip.PickupSiteKey, trip.PickupText),
            PickupText = Clean(trip.PickupText),
            Commodity = Clean(trip.Commodity),
            TemperatureMinC = trip.TemperatureMinC,
            TemperatureMaxC = trip.TemperatureMaxC,
            Notes = Clean(trip.Notes),
            Drops = (trip.Drops ?? [])
                .Where(d => !string.IsNullOrWhiteSpace(d.Text) || !string.IsNullOrWhiteSpace(d.SiteKey))
                .Select(d => MapDrop(d, Resolve(d.SiteKey, d.Text), sitesByKey, sitesByName))
                .ToList()
        }).ToList();

        return Result<BookingRequestReadingDto>.Ok(new BookingRequestReadingDto { Trips = trips, Warnings = warnings });
    }

    private static ProposedDropDto MapDrop(
        ReplyDrop drop,
        ProposedSiteDto? site,
        IReadOnlyDictionary<string, ProposedSiteDto> sitesByKey,
        ILookup<string, ProposedSiteDto> sitesByName)
    {
        var text = Clean(drop.Text) ?? "";

        // The model sometimes holds back on a name that is saved word for word, so an exact match
        // wins in code - but only when exactly one saved location has that name.
        if (site is null && sitesByName[NormalizeName(text)].ToList() is [var exact])
            site = exact;

        var candidates = site is not null
            ? []
            : (drop.CandidateKeys ?? [])
                .Select(key => sitesByKey.GetValueOrDefault(key.Trim()))
                .OfType<ProposedSiteDto>()
                .DistinctBy(s => s.Id)
                .Take(MaxCandidates)
                .ToList();

        return new ProposedDropDto
        {
            Text = text,
            Site = site,
            Candidates = candidates,
            PlannedBoxes = drop.Boxes is > 0 ? drop.Boxes : null,
            PlannedBags = drop.Bags is > 0 ? drop.Bags : null,
            Notes = Clean(drop.Notes)
        };
    }

    private static string NormalizeName(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static Reply? Deserialize(string text)
    {
        // Asked for raw JSON, but models still wrap it in fences or a sentence now and then.
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        try
        {
            return JsonSerializer.Deserialize<Reply>(text[start..(end + 1)], JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static TimeOnly? ParseTime(string? value) =>
        TimeOnly.TryParseExact(value?.Trim(), ["HH:mm", "H:mm", "HH:mm:ss"], CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var time)
            ? time
            : null;

    private static TruckTonnageClass? ParseTruckSize(string? value) =>
        value?.Trim().ToUpperInvariant() switch
        {
            "2T" => TruckTonnageClass.TwoTonner,
            "3T" => TruckTonnageClass.ThreeTonner,
            "4T" => TruckTonnageClass.FourTonner,
            "10T" => TruckTonnageClass.TenTonner,
            _ => null
        };

    private sealed record Reply(List<ReplyTrip>? Trips, List<string>? Warnings);

    private sealed record ReplyTrip(
        string? ServiceDate,
        string? CallTime,
        string? TruckSize,
        string? PickupSiteKey,
        string? PickupText,
        string? Commodity,
        decimal? TemperatureMinC,
        decimal? TemperatureMaxC,
        string? Notes,
        List<ReplyDrop>? Drops);

    private sealed record ReplyDrop(
        string? Text, string? SiteKey, List<string>? CandidateKeys, int? Boxes, int? Bags, string? Notes);
}
