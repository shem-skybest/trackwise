using MockQueryable;
using NSubstitute;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ConvertQuotationToBooking;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class ConvertQuotationToBookingHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();

    private readonly ITenantRepository<Quotation, Guid> quotationRepo =
        Substitute.For<ITenantRepository<Quotation, Guid>>();
    private readonly ITenantRepository<Booking, Guid> bookingRepo =
        Substitute.For<ITenantRepository<Booking, Guid>>();

    private readonly Quotation quotation;
    private readonly ConvertQuotationToBookingHandler sut;

    public ConvertQuotationToBookingHandlerTests()
    {
        quotation = CreateTestQuotation();

        tenantUow.Repository<Quotation>().Returns(quotationRepo);
        tenantUow.Repository<Booking>().Returns(bookingRepo);

        quotationRepo.GetByIdAsync(quotation.Id, Arg.Any<CancellationToken>()).Returns(quotation);
        bookingRepo.Query().Returns(new List<Booking>().BuildMock());

        sut = new ConvertQuotationToBookingHandler(tenantUow);
    }

    [Fact]
    public async Task Handle_QuotationNotFound_Fails()
    {
        var command = new ConvertQuotationToBookingCommand { QuotationId = Guid.NewGuid() };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_QuotationNotApproved_Fails()
    {
        var command = new ConvertQuotationToBookingCommand { QuotationId = quotation.Id };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Approved", result.Error);
        await bookingRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_AlreadyConvertedQuotation_Fails()
    {
        quotation.Approve("manager@test.com");
        bookingRepo.Query().Returns(new List<Booking> { CreateExistingBooking() }.BuildMock());

        var command = new ConvertQuotationToBookingCommand { QuotationId = quotation.Id };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already been converted", result.Error);
    }

    [Fact]
    public async Task Handle_ApprovedQuotation_CreatesPendingBooking()
    {
        quotation.Approve("manager@test.com");

        var command = new ConvertQuotationToBookingCommand
        {
            QuotationId = quotation.Id,
            ContainerTypeId = Guid.NewGuid(),
            Notes = "Handle with care"
        };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await bookingRepo.Received(1).AddAsync(
            Arg.Is<Booking>(b =>
                b.QuotationId == quotation.Id &&
                b.CustomerId == quotation.CustomerId &&
                b.Currency == quotation.Currency &&
                b.Status == BookingStatus.InProgress),
            Arg.Any<CancellationToken>());
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Booking CreateExistingBooking()
    {
        return Booking.CreateDraftFromQuotation(quotation);
    }

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
