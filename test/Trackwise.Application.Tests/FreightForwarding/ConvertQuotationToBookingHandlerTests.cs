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
    public async Task Handle_QuotationNotAccepted_Fails()
    {
        typeof(Quotation).GetProperty(nameof(Quotation.Status))!
            .SetValue(quotation, QuotationStatus.Draft);
        var command = new ConvertQuotationToBookingCommand { QuotationId = quotation.Id };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Accepted", result.Error);
        await bookingRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_AlreadyConvertedQuotation_ReturnsExistingBooking()
    {
        var existing = CreateExistingBooking();
        bookingRepo.Query().Returns(new List<Booking> { existing }.BuildMock());

        var command = new ConvertQuotationToBookingCommand { QuotationId = quotation.Id };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id, result.Value);
        await bookingRepo.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_AcceptedQuotation_CreatesDraftBooking()
    {
        var command = new ConvertQuotationToBookingCommand
        {
            QuotationId = quotation.Id,
            ContainerTypeId = Guid.NewGuid(),
            Notes = "Handle with care",
            RequestedPickupDate = DateTime.UtcNow.Date.AddDays(2)
        };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await bookingRepo.Received(1).AddAsync(
            Arg.Is<Booking>(b =>
                b.QuotationId == quotation.Id &&
                b.CustomerId == quotation.CustomerId &&
                b.Currency == quotation.Currency &&
                b.Status == BookingStatus.Draft &&
                b.RequestedPickupStart == new DateTimeOffset(command.RequestedPickupDate.Value, TimeSpan.Zero)),
            Arg.Any<CancellationToken>());
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RepeatAcceptedQuotation_CreatesAnotherBookingWithNewNumber()
    {
        var existing = CreateExistingBooking();
        bookingRepo.Query().Returns(new List<Booking> { existing }.BuildMock());

        var command = new ConvertQuotationToBookingCommand
        {
            QuotationId = quotation.Id,
            Repeat = true
        };

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(existing.Id, result.Value);
        await bookingRepo.Received(1).AddAsync(
            Arg.Is<Booking>(b =>
                b.QuotationId == quotation.Id &&
                b.BookingNumber != existing.BookingNumber &&
                b.Status == BookingStatus.Draft),
            Arg.Any<CancellationToken>());
    }

    private Booking CreateExistingBooking()
    {
        return Booking.CreateDraftFromQuotation(quotation, 1);
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
            branchId: Guid.NewGuid(),
            referenceNumber: "QT-TEST-001",
            currency: "USD",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: lane,
            cargoSpec: cargo);

        typeof(Quotation).GetProperty(nameof(Quotation.Status))!
            .SetValue(quotation, QuotationStatus.Accepted);
        return quotation;
    }
}
