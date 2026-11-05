using Trackwise.Application.Modules.Compliance.Eld.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using NSubstitute;
using Xunit;
using Trackwise.Application.Modules.Compliance.Eld.Queries;

namespace Trackwise.Application.Tests.Eld;

public class HosLimitsEndpointTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly GetHosLimitsHandler sut;

    public HosLimitsEndpointTests()
    {
        sut = new GetHosLimitsHandler(tenantUow);
    }

    [Fact]
    public async Task Handle_USTenant_ReturnsFmcsaLimits()
    {
        WireTenantRegion(Region.US);

        var result = await sut.Handle(new GetHosLimitsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HosLimits.FmcsaCode, result.Value!.RuleSetCode);
        Assert.Equal(11 * 60, result.Value.MaxDailyDrivingMinutes);
        Assert.Equal(14 * 60, result.Value.MaxDailyOnDutyMinutes);
        Assert.Equal(70 * 60, result.Value.MaxBiweeklyDrivingMinutes);
        Assert.Null(result.Value.MaxContinuousDrivingMinutes);
        Assert.Equal(8, result.Value.CycleDays);
    }

    [Fact]
    public async Task Handle_EUTenant_ReturnsEU561Limits()
    {
        WireTenantRegion(Region.EU);

        var result = await sut.Handle(new GetHosLimitsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(HosLimits.EU561Code, result.Value!.RuleSetCode);
        Assert.Equal(9 * 60, result.Value.MaxDailyDrivingMinutes);
        Assert.Equal(56 * 60, result.Value.MaxWeeklyDrivingMinutes);
        Assert.Equal(90 * 60, result.Value.MaxBiweeklyDrivingMinutes);
        Assert.Equal(270, result.Value.MaxContinuousDrivingMinutes);
        Assert.Equal(270, result.Value.RequiredBreakAfterMinutes);
        Assert.Equal(11 * 60, result.Value.MinDailyRestMinutes);
        Assert.Equal(45 * 60, result.Value.MinWeeklyRestMinutes);
        Assert.Equal(14, result.Value.CycleDays);
    }

    [Theory]
    [InlineData(Region.US, HosLimits.FmcsaCode)]
    [InlineData(Region.EU, HosLimits.EU561Code)]
    public void RuleSetSelector_CodeFor_ReturnsExpectedCode(Region region, string expected)
    {
        Assert.Equal(expected, RuleSetSelector.CodeFor(region));
    }

    private void WireTenantRegion(Region region)
    {
        var tenant = new Tenant
        {
            Name = "test",
            CompanyAddress = new Address
            {
                StreetAddress = "1",
                CityOrMunicipality = "x",
                Province = "x",
                Region = "",
                Zipcode = "0",
                Country = region == Region.EU ? "DE" : "US"
            },
            ConnectionString = "x",
            BillingEmail = "x@y.z",
            Settings = new TenantSettings { Region = region }
        };
        tenantUow.GetCurrentTenant().Returns(tenant);
    }
}
