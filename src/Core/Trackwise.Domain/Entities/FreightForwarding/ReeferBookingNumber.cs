using System.Text.RegularExpressions;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Reads a Cold Chain booking number typed in by hand, the way staff copy it from nSFS or Monday:
/// "BKN-RFV1331-26-M", "BKN-RFV 1331" or just "1331". Always gives nSFS's printed form.
/// </summary>
public static partial class ReeferBookingNumber
{
    /// <param name="typed">What was typed.</param>
    /// <param name="today">The tenant's local date, for the year when the number is typed without one.</param>
    /// <exception cref="InvalidOperationException">The text isn't a booking number.</exception>
    public static string Normalize(string typed, DateOnly today)
    {
        var match = Typed().Match(typed);
        if (!match.Success || !long.TryParse(match.Groups["number"].Value, out var number) || number == 0)
        {
            throw new InvalidOperationException(
                $"'{typed.Trim()}' isn't a booking number. Type it as nSFS shows it, e.g. BKN-RFV1331-26-M, or just 1331.");
        }

        var year = match.Groups["year"].Success ? match.Groups["year"].Value : (today.Year % 100).ToString("D2");
        var branch = match.Groups["branch"].Success
            ? match.Groups["branch"].Value.ToUpperInvariant()
            : WorkfileSeries.NsfsBranch;

        return $"{WorkfileSeries.Reefer}{number:D4}-{year}-{branch}";
    }

    [GeneratedRegex(
        @"^\s*(?:BKN\s*-?\s*RFV)?\s*-?\s*(?<number>\d{1,6})(?:\s*-\s*(?<year>\d{2})(?:\s*-\s*(?<branch>[A-Za-z]))?)?\s*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex Typed();
}
