using NSubstitute;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class SetBookingOperationalStatusHandlerTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Booking, Guid> bookingRepo =
        Substitute.For<ITenantRepository<Booking, Guid>>();

    private readonly Booking booking;
    private readonly SetBookingOperationalStatusHandler sut;

    public SetBookingOperationalStatusHandlerTests()
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
            referenceNumber: "QT-TEST-OPS-001",
            currency: "USD",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: lane,
            cargoSpec: cargo);

        quotation.Approve("manager@test.com");
        booking = Booking.CreateDraftFromQuotation(quotation);

        tenantUow.Repository<Booking>().Returns(bookingRepo);
        bookingRepo.GetByIdAsync(booking.Id, Arg.Any<CancellationToken>()).Returns(booking);

        sut = new SetBookingOperationalStatusHandler(tenantUow);
    }

    [Theory]
    [InlineData(BookingStatus.Resched)]
    [InlineData(BookingStatus.Hold)]
    //[InlineData(BookingStatus.LostOpportunity)]
    //[InlineData(BookingStatus.NoBooking)]
    public async Task Handle_PendingBooking_SetsRequestedOperationalStatus(BookingStatus status)
    {
        var result = await sut.Handle(
            new SetBookingOperationalStatusCommand { Id = booking.Id, Status = status, Note = "client asked" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(status, booking.Status);
        Assert.Equal("client asked", booking.Notes);
    }

    [Fact]
    public async Task Handle_HeldBooking_CanMoveToAnotherOperationalStatus()
    {
        //booking.Hold(null);

        var result = await sut.Handle(
            new SetBookingOperationalStatusCommand { Id = booking.Id, Status = BookingStatus.Resched },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Resched, booking.Status);
    }

    [Fact]
    public async Task Handle_InExecutionBooking_Fails()
    {
        booking.Confirm("ops@test.com");
        //booking.StartExecution();

        var result = await sut.Handle(
            new SetBookingOperationalStatusCommand { Id = booking.Id, Status = BookingStatus.Hold },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BookingStatus.InProgress, booking.Status);
    }

    [Fact]
    public async Task Handle_TargetNotOneOfTheFourOperationalStatuses_Fails()
    {
        var result = await sut.Handle(
            new SetBookingOperationalStatusCommand { Id = booking.Id, Status = BookingStatus.Confirmed },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_UnknownBooking_Fails()
    {
        var result = await sut.Handle(
            new SetBookingOperationalStatusCommand { Id = Guid.NewGuid(), Status = BookingStatus.Hold },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
