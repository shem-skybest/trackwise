using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Billing;

/// <summary>
/// Cut-offs as Cold Chain bills them (Monday's MONTHLY BILLING UPDATES, 2026): 1-15 and 16-end for
/// most clients, Monday to Saturday for Bayview and Innovatrade, one trip at a time for the rest.
/// </summary>
public class BillingPeriodTests
{
    [Theory]
    [InlineData("2026-09-01", "2026-09-01", "2026-09-15")]
    [InlineData("2026-09-15", "2026-09-01", "2026-09-15")]
    [InlineData("2026-09-16", "2026-09-16", "2026-09-30")]
    [InlineData("2026-02-20", "2026-02-16", "2026-02-28")]
    [InlineData("2028-02-20", "2028-02-16", "2028-02-29")]
    public void For_SemiMonthly_SplitsTheMonthOnThe15th(string day, string start, string end)
    {
        var period = BillingPeriod.For(BillingCycle.SemiMonthly, DateOnly.Parse(day));

        Assert.Equal(new BillingPeriod(DateOnly.Parse(start), DateOnly.Parse(end)), period);
    }

    [Theory]
    [InlineData("2026-08-17", "2026-08-17", "2026-08-22")] // Monday
    [InlineData("2026-08-22", "2026-08-17", "2026-08-22")] // Saturday
    [InlineData("2026-08-23", "2026-08-17", "2026-08-23")] // Sunday closes its own week
    public void For_Weekly_RunsMondayToSaturday(string day, string start, string end)
    {
        var period = BillingPeriod.For(BillingCycle.Weekly, DateOnly.Parse(day));

        Assert.Equal(new BillingPeriod(DateOnly.Parse(start), DateOnly.Parse(end)), period);
    }

    [Fact]
    public void For_PerTrip_IsTheTripsDay()
    {
        var day = new DateOnly(2026, 9, 2);

        Assert.Equal(new BillingPeriod(day, day), BillingPeriod.For(BillingCycle.PerTrip, day));
    }

    [Fact]
    public void For_Monthly_IsTheCalendarMonth()
    {
        var period = BillingPeriod.For(BillingCycle.Monthly, new DateOnly(2026, 4, 9));

        Assert.Equal(new BillingPeriod(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30)), period);
    }
}
