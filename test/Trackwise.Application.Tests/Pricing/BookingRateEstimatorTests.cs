using MockQueryable;
using NSubstitute;
using Trackwise.Application.Modules.Operations.RateAgreements;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Pricing;

/// <summary>
/// A booking is estimated on the inputs its trip will later be priced on: the agreement and diesel
/// price of the service date, the drops' saved sites, and the distance from the pickup warehouse.
/// </summary>
public class BookingRateEstimatorTests
{
    private static readonly DateOnly September24 = new(2026, 9, 24);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly List<RateAgreement> agreements = [];
    private readonly List<Location> sites = [];
    private readonly List<DieselPrice> dieselPrices = [new() { EffectiveFrom = new DateOnly(2026, 9, 1), PricePerLitre = 90 }];
    private readonly Guid customerId = Guid.NewGuid();

    public BookingRateEstimatorTests()
    {
        var agreementRepo = Substitute.For<ITenantRepository<RateAgreement, Guid>>();
        var locationRepo = Substitute.For<ITenantRepository<Location, Guid>>();
        var dieselRepo = Substitute.For<ITenantRepository<DieselPrice, Guid>>();

        tenantUow.Repository<RateAgreement>().Returns(agreementRepo);
        tenantUow.Repository<Location>().Returns(locationRepo);
        tenantUow.Repository<DieselPrice>().Returns(dieselRepo);

        agreementRepo.Query().Returns(_ => agreements.BuildMock());
        locationRepo.Query().Returns(_ => sites.BuildMock());
        dieselRepo.Query().Returns(_ => dieselPrices.BuildMock());
    }

    private RateAgreement Agreement(params RateLineSpec[] lines)
    {
        var agreement = RateAgreement.Create(
            customerId, "HFI rates", new DateOnly(2026, 3, 13), null, new RateAgreementTerms(FuelAdjustmentMethod.DieselTierSurcharge, ExtraDropCharges: [(null, 300m)]), null);
        agreement.ReplaceLines(lines);
        agreement.Approve("manager", DateTime.UtcNow);
        agreements.Add(agreement);
        return agreement;
    }

    private Location Site(string name, string city, string province, Guid? owner, double? lat = null, double? lon = null)
    {
        var site = new Location
        {
            Id = Guid.NewGuid(),
            Code = name.ToUpperInvariant(),
            Name = name,
            Type = LocationType.DeliveryPoint,
            CustomerId = owner,
            Latitude = lat,
            Longitude = lon,
            Address = new Address
            {
                StreetAddress = name, CityOrMunicipality = city, Province = province, Region = "NCR", Zipcode = "", Country = "PH"
            }
        };
        sites.Add(site);
        return site;
    }

    private static ColdChainBookingPlan Plan(Guid? pickup, params Location[] drops) => new(
        September24, new TimeOnly(1, 0), TruckTonnageClass.TwoTonner, pickup, null, null, null, null,
        drops.Select(d => new BookingDropSpec(d.Id, 40, null, null)).ToList());

    [Fact]
    public async Task EstimateAsync_TwoDropsInTown_ChargesTheTownRateFuelAndOneExtraDrop()
    {
        Agreement(new RateLineSpec(
            "QUEZON CITY", TruckTonnageClass.TwoTonner, 11200, null, null, "Quezon City", "Metro Manila", null, null, [(85m, 1250m)]));
        var schoolA = Site("Bagong Silangan ES", "Quezon City", "Metro Manila", customerId);
        var schoolB = Site("Payatas ES", "Quezon City", "Metro Manila", customerId);
        var plan = Plan(null, schoolA, schoolB);

        var prepared = await BookingRateEstimator.PrepareAsync(tenantUow, customerId, plan, new HashSet<Guid>(), CancellationToken.None);
        var (quote, estimate) = await BookingRateEstimator.EstimateAsync(tenantUow, prepared.Value!, plan, CancellationToken.None);

        Assert.NotNull(quote);
        Assert.Null(estimate.Error);
        Assert.Equal(11200, estimate.BaseAmount);
        Assert.Equal(1250, estimate.FuelAmount);
        Assert.Equal(300, estimate.ExtraDropAmount);
        Assert.Equal(12750, estimate.Total);
        Assert.Equal(90, estimate.DieselPrice);
    }

    [Fact]
    public async Task EstimateAsync_DistanceBandCard_ReadsTheDistanceFromThePickupWarehouse()
    {
        Agreement(
            new RateLineSpec("10 - 40 KM", TruckTonnageClass.TwoTonner, 6400, 10, 40, null, null, null, null, [(85m, 220m)]),
            new RateLineSpec("41 - 60 KM", TruckTonnageClass.TwoTonner, 7200, 41, 60, null, null, null, null, [(85m, 300m)]));
        // About 50 km apart, straight line.
        var warehouse = Site("PFI warehouse", "Valenzuela", "Metro Manila", customerId, 14.70, 120.98);
        var outlet = Site("Calamba outlet", "Calamba", "Laguna", customerId, 14.25, 121.00);

        var plan = Plan(warehouse.Id, outlet);
        var prepared = await BookingRateEstimator.PrepareAsync(tenantUow, customerId, plan, new HashSet<Guid>(), CancellationToken.None);
        var (_, estimate) = await BookingRateEstimator.EstimateAsync(tenantUow, prepared.Value!, plan, CancellationToken.None);

        Assert.Equal("41 - 60 KM", estimate.LineLabel);
        Assert.Equal(7500, estimate.Total);
    }

    [Fact]
    public async Task EstimateAsync_DistanceBandCardWithoutAPinnedPickup_SaysWhyInsteadOfFailing()
    {
        Agreement(new RateLineSpec("10 - 40 KM", TruckTonnageClass.TwoTonner, 6400, 10, 40, null, null, null, null, [(85m, 220m)]));
        var outlet = Site("Calamba outlet", "Calamba", "Laguna", customerId, 14.25, 121.00);

        var plan = Plan(null, outlet);
        var prepared = await BookingRateEstimator.PrepareAsync(tenantUow, customerId, plan, new HashSet<Guid>(), CancellationToken.None);
        var (quote, estimate) = await BookingRateEstimator.EstimateAsync(tenantUow, prepared.Value!, plan, CancellationToken.None);

        Assert.Null(quote);
        Assert.Null(estimate.Total);
        Assert.Contains("no rate", estimate.Error);
    }

    [Fact]
    public async Task PrepareAsync_AnotherClientsOutlet_IsRefused()
    {
        Agreement(new RateLineSpec(
            "QUEZON CITY", TruckTonnageClass.TwoTonner, 11200, null, null, "Quezon City", "Metro Manila", null, null, null));
        var foreign = Site("Someone else's store", "Quezon City", "Metro Manila", Guid.NewGuid());

        var prepared = await BookingRateEstimator.PrepareAsync(
            tenantUow, customerId, Plan(null, foreign), new HashSet<Guid>(), CancellationToken.None);

        Assert.False(prepared.IsSuccess);
        Assert.Contains("another customer", prepared.Error);
    }

    [Fact]
    public async Task PrepareAsync_NoAgreementInForce_Fails()
    {
        var school = Site("Payatas ES", "Quezon City", "Metro Manila", customerId);

        var prepared = await BookingRateEstimator.PrepareAsync(
            tenantUow, customerId, Plan(null, school), new HashSet<Guid>(), CancellationToken.None);

        Assert.False(prepared.IsSuccess);
        Assert.Contains("no approved rate agreement", prepared.Error);
    }
}
