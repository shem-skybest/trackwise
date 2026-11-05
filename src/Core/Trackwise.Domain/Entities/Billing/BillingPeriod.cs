using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>A client's billing period - the cut-off printed on the batch summary.</summary>
public readonly record struct BillingPeriod(DateOnly Start, DateOnly End)
{
    public bool Contains(DateOnly day) => day >= Start && day <= End;

    /// <summary>
    /// The period of <paramref name="cycle" /> that <paramref name="day" /> falls in, as Cold Chain
    /// cuts them: 1-15 and 16-month end, Monday to Saturday (a Sunday trip closes its own week), the
    /// calendar month, or the trip's day alone.
    /// </summary>
    public static BillingPeriod For(BillingCycle cycle, DateOnly day) => cycle switch
    {
        BillingCycle.SemiMonthly when day.Day <= 15 => new(new(day.Year, day.Month, 1), new(day.Year, day.Month, 15)),
        BillingCycle.SemiMonthly => new(new(day.Year, day.Month, 16), MonthEnd(day)),
        BillingCycle.Weekly => Week(day),
        BillingCycle.Monthly => new(new(day.Year, day.Month, 1), MonthEnd(day)),
        _ => new(day, day)
    };

    private static BillingPeriod Week(DateOnly day)
    {
        var monday = day.AddDays(-(((int)day.DayOfWeek + 6) % 7));
        return new(monday, day.DayOfWeek == DayOfWeek.Sunday ? day : monday.AddDays(5));
    }

    private static DateOnly MonthEnd(DateOnly day) =>
        new(day.Year, day.Month, DateTime.DaysInMonth(day.Year, day.Month));
}
