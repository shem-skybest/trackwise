using MockQueryable;
using NSubstitute;
using Trackwise.Application.Abstractions.Modules.Operations.FreightForwarding;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// A standing order (HFI books every Monday) copies one booking onto chosen days. Each copy is a
/// booking of its own, and a day the agreement doesn't cover stops the whole run.
/// </summary>
public class RepeatColdChainBookingHandlerTests
{
    private static readonly DateOnly Oct5 = new(2026, 10, 5);
    private static readonly DateOnly Oct12 = new(2026, 10, 12);
    private static readonly DateOnly Oct19 = new(2026, 10, 19);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Booking, Guid> bookingRepo = Substitute.For<ITenantRepository<Booking, Guid>>();
    private readonly IWorkfileReferenceGenerator referenceGenerator = Substitute.For<IWorkfileReferenceGenerator>();
    private readonly List<Booking> added = [];
    private readonly TenantSettings settings = new() { Currency = CurrencyCode.PHP, Timezone = "Asia/Manila" };
    private bool? committed;
    private int nextNumber = 1400;

    private readonly Location warehouse = Site("HFI Warehouse", LocationType.Warehouse);
    private readonly Location outlet = Site("SM Fairview", LocationType.DeliveryPoint);
    private readonly RateAgreement agreement;
    private readonly Booking source;
    private readonly RepeatColdChainBookingHandler sut;

    public RepeatColdChainBookingHandlerTests()
    {
        agreement = Agreement(new DateOnly(2026, 6, 1), new DateOnly(2026, 10, 15));
        source = Booking.CreateFromRateAgreement(agreement, "BKN-RFV-001381", new ColdChainBookingPlan(
            new DateOnly(2026, 9, 28), new TimeOnly(1, 0), TruckTonnageClass.TwoTonner, warehouse.Id,
            "Frozen goods", -18, -15, "Call the guard", [new BookingDropSpec(outlet.Id, 60, null, null)]));
        source.Confirm("cs");

        tenantUow.GetCurrentTenant().Returns(_ => new Tenant
        {
            Name = "ph",
            ConnectionString = "test",
            BillingEmail = "billing@test.com",
            CompanyAddress = new Address { StreetAddress = "1 Test St", CityOrMunicipality = "Pasay", Province = "Metro Manila", Region = "NCR", Zipcode = "1300", Country = "PH" },
            Settings = settings
        });
        tenantUow.Repository<Booking>().Returns(bookingRepo);
        bookingRepo.GetByIdAsync(source.Id, Arg.Any<CancellationToken>()).Returns(source);
        bookingRepo.AddAsync(Arg.Do<Booking>(added.Add), Arg.Any<CancellationToken>());

        var agreementRepo = Substitute.For<ITenantRepository<RateAgreement, Guid>>();
        agreementRepo.Query().Returns(_ => new List<RateAgreement> { agreement }.BuildMock());
        tenantUow.Repository<RateAgreement>().Returns(agreementRepo);

        var locationRepo = Substitute.For<ITenantRepository<Location, Guid>>();
        locationRepo.Query().Returns(_ => new List<Location> { warehouse, outlet }.BuildMock());
        tenantUow.Repository<Location>().Returns(locationRepo);

        var dieselRepo = Substitute.For<ITenantRepository<DieselPrice, Guid>>();
        dieselRepo.Query().Returns(_ => new List<DieselPrice>().BuildMock());
        tenantUow.Repository<DieselPrice>().Returns(dieselRepo);

        referenceGenerator.NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => $"{call.Arg<string>()}-{nextNumber++:D6}");

        tenantUow.ExecuteInTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<TransactionOutcome<Result<List<RepeatedBookingDto>>>>>>(),
                Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var work = call.Arg<Func<CancellationToken, Task<TransactionOutcome<Result<List<RepeatedBookingDto>>>>>>();
                var outcome = await work(CancellationToken.None);
                committed = outcome.Commit;
                return outcome.Value;
            });

        sut = new RepeatColdChainBookingHandler(tenantUow, referenceGenerator);
    }

    private RepeatColdChainBookingCommand Repeat(bool confirm, params DateOnly[] dates) => new()
    {
        BookingId = source.Id,
        Dates = [.. dates],
        Confirm = confirm,
        ConfirmedBy = "cs-user"
    };

    [Fact]
    public async Task Handle_TwoMondays_MakesOneNumberedPendingBookingPerDay()
    {
        var result = await sut.Handle(Repeat(false, Oct12, Oct5), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal([Oct5, Oct12], result.Value!.Select(r => r.ServiceDate));
        Assert.Equal(["BKN-RFV-001400", "BKN-RFV-001401"], result.Value!.Select(r => r.ReferenceNumber));
        Assert.Equal(2, added.Count);
        Assert.All(added, b =>
        {
            Assert.Equal(BookingStatus.InProgress, b.Status);
            Assert.Equal(source.CustomerId, b.CustomerId);
            Assert.Equal(outlet.Id, Assert.Single(b.Drops).SiteId);
            Assert.Equal("Call the guard", b.Notes);
        });
        Assert.True(committed);
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NumbersTypedFromNsfs_CopiesWaitForTheirOwnNumber()
    {
        settings.ColdChainBookingNumbersTyped = true;

        var result = await sut.Handle(Repeat(false, Oct5, Oct12), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        Assert.All(result.Value!, r => Assert.Null(r.ReferenceNumber));
        Assert.All(added, b => Assert.Null(b.ReferenceNumber));
        await referenceGenerator.DidNotReceive().NextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Confirm_CopiesAreConfirmedByTheUser()
    {
        var result = await sut.Handle(Repeat(true, Oct5), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error);
        var copy = Assert.Single(added);
        Assert.Equal(BookingStatus.Confirmed, copy.Status);
        Assert.Equal("cs-user", copy.ConfirmedBy);
    }

    [Fact]
    public async Task Handle_DayAfterTheAgreementEnds_FailsNamingTheDay_AndKeepsNothing()
    {
        var result = await sut.Handle(Repeat(false, Oct5, Oct12, Oct19), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("2026-10-19", result.Error);
        Assert.False(committed);
        await tenantUow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardingBooking_Fails()
    {
        var quotation = CreateTestQuotation();
        quotation.Approve("manager");
        var forwarding = Booking.CreateDraftFromQuotation(quotation);
        bookingRepo.GetByIdAsync(forwarding.Id, Arg.Any<CancellationToken>()).Returns(forwarding);

        var command = Repeat(false, Oct5);
        command.BookingId = forwarding.Id;
        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(added);
    }

    private static RateAgreement Agreement(DateOnly from, DateOnly to)
    {
        var rates = RateAgreement.Create(
            Guid.NewGuid(), "HFI rates", from, to,
            new RateAgreementTerms(FuelAdjustmentMethod.DieselTierSurcharge, ExtraDropCharges: [(null, 300m)]), null);
        rates.ReplaceLines([
            new RateLineSpec("QUEZON CITY", TruckTonnageClass.TwoTonner, 11200, null, null, "Quezon City", "Metro Manila", null, null, [(85m, 1250m)])
        ]);
        rates.Approve("manager", DateTime.UtcNow);
        return rates;
    }

    private static Location Site(string name, LocationType type) => new()
    {
        Code = name.ToUpperInvariant().Replace(' ', '-'),
        Name = name,
        Type = type
    };

    private static Quotation CreateTestQuotation()
    {
        var lane = LaneTransit.PointToPoint(
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            pickupStart: DateTimeOffset.UtcNow,
            deliveryEnd: DateTimeOffset.UtcNow.AddDays(1),
            distanceKm: 100m);

        var cargo = CargoSpecification.Create(
            commodity: "General Cargo",
            totalWeightKg: 1000m,
           palletCount: 2,
           declaredCargoValue: new Money { Amount = 5000m, Currency = "USD" },
            requiresActiveRefrigeration: false);

        var quotation = Quotation.Create(
            customerId: Guid.NewGuid(),
            referenceNumber: "QT-TEST-001",
            currency: "USD",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: lane,
            cargoSpec: cargo);

        quotation.Approve("manager@test.com");
        return quotation;
    }
}
