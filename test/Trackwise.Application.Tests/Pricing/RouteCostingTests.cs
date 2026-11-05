using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Pricing;

/// <summary>
/// The route costing, checked against the rows of Cold Chain's Monday costing boards where the
/// assumptions match them.
/// </summary>
public class RouteCostingTests
{
    [Fact]
    public void Compute_MondaysCa2TonnerFortyKmRow_GivesItsSellingRate()
    {
        // CA - 2 TONNER COSTING, "40 km": 10 + 15 + 15 km, ÷3, ₱75 a litre, salary 2,273.
        var assumptions = RouteCosting.DefaultsFor(TruckTonnageClass.TwoTonner) with { Buffer = 0, RoundUpTo = 0 };

        var result = RouteCosting.Compute(10, 15, 15, 75m, assumptions);

        Assert.Equal(40, result.TotalKm);
        Assert.Equal(1000, result.Fuel);
        Assert.Equal(2273, result.Crew);
        Assert.Equal(981.9m, result.Admin);
        Assert.Equal(1276.47m, result.Markup);
        Assert.Equal(5531.37m, result.SellingRate);
    }

    [Fact]
    public void Compute_MondaysCa4TonnerFortyKmRow_GivesItsSellingRate()
    {
        // CA - 4 TONNER COSTING, "40 km": ÷2, ₱100 a litre, salary 2,573, markup 60%.
        var assumptions = RouteCosting.DefaultsFor(TruckTonnageClass.FourTonner) with { Buffer = 0, RoundUpTo = 0 };

        var result = RouteCosting.Compute(10, 15, 15, 100m, assumptions);

        Assert.Equal(2000, result.Fuel);
        Assert.Equal(2573, result.Crew);
        Assert.Equal(9511.84m, result.SellingRate);
        Assert.Equal(Math.Round((result.Expenses + result.Admin) * 0.6m, 2), result.Markup);
    }

    [Fact]
    public void Compute_TheBuffer_IsAddedAfterMarkup_ThenRoundedUpToTheNextHundred()
    {
        var result = RouteCosting.Compute(10, 15, 15, 75m, RouteCosting.DefaultsFor(TruckTonnageClass.TwoTonner));

        Assert.Equal(6531.37m, result.BeforeRounding);
        Assert.Equal(6600, result.SellingRate);
    }

    [Theory]
    [InlineData(true, 300)]
    [InlineData(false, 150)]
    public void DefaultsFor_TheDriversAllowance_FollowsSeniority(bool senior, decimal allowance)
    {
        Assert.Equal(allowance, RouteCosting.DefaultsFor(TruckTonnageClass.TwoTonner, senior).DriverAllowance);
    }
}
