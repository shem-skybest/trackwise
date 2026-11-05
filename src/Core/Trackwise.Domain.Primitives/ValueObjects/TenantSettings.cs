using System.ComponentModel.DataAnnotations.Schema;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Primitives.ValueObjects;

/// <summary>
/// Regional and localization settings for a tenant.
/// </summary>
[ComplexType]
public record TenantSettings
{
    public Region Region { get; set; } = Region.US;
    public DistanceUnit DistanceUnit { get; set; } = DistanceUnit.Miles;
    public CurrencyCode Currency { get; set; } = CurrencyCode.USD;
    public DateFormatType DateFormat { get; set; } = DateFormatType.US;
    public string Timezone { get; set; } = "America/New_York";
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Pounds;
    public VolumeUnit VolumeUnit { get; set; } = VolumeUnit.Gallons;
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Fahrenheit;

    /// <summary>Drives the UX preset (hidden team surfaces, dashboard layout) and the AI dispatch prompt.</summary>
    public OperatingMode OperatingMode { get; set; } = OperatingMode.Fleet;

    /// <summary>
    /// ISO 639-1 language code used as the tenant's UI/document default. Falls back to "en".
    /// </summary>
    public string Language { get; set; } = "en";

    /// <summary>
    /// Whether AI (LLM API calls) is enabled for this tenant. Null/true = enabled, false = blocked.
    /// Used to prevent LLM usage on demo/test tenants in production.
    /// Bypassed in development environments.
    /// </summary>
    public bool? AIEnabled { get; set; }

    /// <summary>
    /// Minimum broker credit score (0-100) required to book a load-board load.
    /// Null disables the gate. A missing score never blocks; inactive FMCSA authority always does.
    /// </summary>
    public int? MinBrokerCreditScore { get; set; }

    /// <summary>
    /// Owner-set: pause AI at the weekly budget instead of billing metered overage (default).
    /// </summary>
    public bool BlockAIOverage { get; set; }

    /// <summary>
    /// Fallback minimum rate per mile the rate-negotiation agent may accept when no lane rule matches.
    /// Null leaves the agent without a floor.
    /// </summary>
    public decimal? DefaultRateFloorPerMile { get; set; }

    /// <summary>
    /// Cold Chain booking numbers are typed in from nSFS, which still issues them while the two run
    /// side by side, instead of TrackWise issuing its own. Set only through its own command:
    /// <c>UpdateTenant</c> keeps the stored value, since the company form doesn't send it.
    /// </summary>
    public bool ColdChainBookingNumbersTyped { get; set; }

    /// <summary>
    /// Sets <paramref name="region" /> with its currency, units, date format and time zone, so a PH
    /// tenant starts on PHP, kilometres and Manila time instead of the US defaults. Other settings
    /// are left as they are.
    /// </summary>
    public void ApplyRegionDefaults(Region region)
    {
        Region = region;
        switch (region)
        {
            case Region.PH:
                Currency = CurrencyCode.PHP;
                ApplyMetricUnits();
                DateFormat = DateFormatType.US;
                Timezone = "Asia/Manila";
                break;
            case Region.EU:
                Currency = CurrencyCode.EUR;
                ApplyMetricUnits();
                DateFormat = DateFormatType.European;
                Timezone = "Europe/Berlin";
                break;
            default:
                Currency = CurrencyCode.USD;
                DistanceUnit = DistanceUnit.Miles;
                WeightUnit = WeightUnit.Pounds;
                VolumeUnit = VolumeUnit.Gallons;
                TemperatureUnit = TemperatureUnit.Fahrenheit;
                DateFormat = DateFormatType.US;
                Timezone = "America/New_York";
                break;
        }
    }

    private void ApplyMetricUnits()
    {
        DistanceUnit = DistanceUnit.Kilometers;
        WeightUnit = WeightUnit.Kilograms;
        VolumeUnit = VolumeUnit.Liters;
        TemperatureUnit = TemperatureUnit.Celsius;
    }

    /// <summary>
    /// The tenant's calendar date at <paramref name="utc" />: a dispatcher's "today" in Manila starts
    /// eight hours before UTC's. An unrecognised time zone id falls back to the UTC date.
    /// </summary>
    public DateOnly LocalDate(DateTime utc)
    {
        var instant = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.TryFindSystemTimeZoneById(Timezone, out var zone)
            ? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(instant, zone))
            : DateOnly.FromDateTime(instant);
    }

    /// <summary>
    /// The UTC instant of a local date and time in the tenant's zone: a 01:00 call time in Manila is
    /// 17:00 UTC the day before. An unrecognised time zone id treats the local time as UTC.
    /// </summary>
    public DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        return TimeZoneInfo.TryFindSystemTimeZoneById(Timezone, out var zone)
            ? TimeZoneInfo.ConvertTimeToUtc(local, zone)
            : DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }
}
