using Trackwise.Domain.Core;
using System.Globalization;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>
/// One value changed on a trip after it was recorded: which field, what it was, what it became, and
/// why. Append-only - the stop keeps who first entered the event, and these rows hold every change
/// since, so a figure that moved can always be traced.
/// </summary>
public class TripCorrection : Entity, ITenantEntity
{
    public Guid TripId { get; private set; }

    /// <summary>The stop corrected; null for the trip's garage times.</summary>
    public Guid? StopId { get; private set; }

    public TripCorrectionField Field { get; private set; }

    /// <summary>
    /// The value before, as text: a time in ISO 8601 UTC, a number in invariant culture, an enum by
    /// name. Null when nothing was recorded.
    /// </summary>
    public string? OldValue { get; private set; }

    /// <summary>The value after, in the same form as <see cref="OldValue" />.</summary>
    public string? NewValue { get; private set; }

    public string Reason { get; private set; } = "";

    public string? CorrectedBy { get; private set; }

    public DateTime CorrectedAt { get; private set; }

    internal static TripCorrection Create(
        Guid tripId,
        Guid? stopId,
        TripCorrectionField field,
        string? oldValue,
        string? newValue,
        string reason,
        string? correctedBy,
        DateTime correctedAt) =>
        new()
        {
            TripId = tripId,
            StopId = stopId,
            Field = field,
            OldValue = oldValue,
            NewValue = newValue,
            Reason = reason,
            CorrectedBy = correctedBy,
            CorrectedAt = correctedAt
        };

    internal static string? Format(DateTime? value) =>
        value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    internal static string? Format(decimal? value) => value?.ToString("0.##", CultureInfo.InvariantCulture);

    internal static string? Format(int? value) => value?.ToString(CultureInfo.InvariantCulture);

    internal static string? Format<TEnum>(TEnum? value) where TEnum : struct, Enum => value?.ToString();
}
