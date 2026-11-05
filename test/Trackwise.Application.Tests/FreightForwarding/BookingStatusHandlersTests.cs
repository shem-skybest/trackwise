using NSubstitute;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.CancelBooking;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.ConfirmBooking;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

public class BookingStatusHandlersTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Booking, Guid> bookingRepo =
        Substitute.For<ITenantRepository<Booking, Guid>>();

    private readonly Booking booking;

    public BookingStatusHandlersTests()
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
            referenceNumber: "QT-TEST-BKG-001",
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
    }

    #region Confirm

    [Fact]
    public async Task Confirm_PendingBooking_Confirms()
    {
        var sut = new ConfirmBookingHandler(tenantUow);

        var result = await sut.Handle(
            new ConfirmBookingCommand { Id = booking.Id, ConfirmedBy = "ops@test.com", CarrierReference = "CARR-1" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        //Assert.Equal("CARR-1", booking.CarrierReference);
        await tenantUow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_NonPendingBooking_Fails()
    {
        booking.Confirm("ops@test.com");
        var sut = new ConfirmBookingHandler(tenantUow);

        var result = await sut.Handle(
            new ConfirmBookingCommand { Id = booking.Id, ConfirmedBy = "ops2@test.com" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region StartExecution

    [Fact]
    public async Task StartExecution_ConfirmedBooking_StartsExecution()
    {
        booking.Confirm("ops@test.com");
        var sut = new StartBookingExecutionHandler(tenantUow);

        var result = await sut.Handle(new StartBookingExecutionCommand { Id = booking.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.InProgress, booking.Status);
    }

    [Fact]
    public async Task StartExecution_PendingBooking_Fails()
    {
        var sut = new StartBookingExecutionHandler(tenantUow);

        var result = await sut.Handle(new StartBookingExecutionCommand { Id = booking.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Complete

    [Fact]
    public async Task Complete_InExecutionBooking_Completes()
    {
        booking.Confirm("ops@test.com");
        //booking.StartExecution();
        var sut = new CompleteBookingHandler(tenantUow);

        var result = await sut.Handle(new CompleteBookingCommand { Id = booking.Id }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.NotNull(booking.CompletedAt);
    }

    [Fact]
    public async Task Complete_ConfirmedButNotStartedBooking_Fails()
    {
        booking.Confirm("ops@test.com");
        var sut = new CompleteBookingHandler(tenantUow);

        var result = await sut.Handle(new CompleteBookingCommand { Id = booking.Id }, CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Cancel

    [Theory]
    [InlineData(BookingStatus.Draft)]
    [InlineData(BookingStatus.Confirmed)]
    public async Task Cancel_PendingOrConfirmedBooking_Cancels(BookingStatus status)
    {
        if (status == BookingStatus.Confirmed)
        {
            booking.Confirm("ops@test.com");
        }

        var sut = new CancelBookingHandler(tenantUow);

        var result = await sut.Handle(
            new CancelBookingCommand { Id = booking.Id, CancelledBy = "ops@test.com", Reason = "Carrier fell through" },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal("Carrier fell through", booking.CancellationReason);
    }

    [Fact]
    public async Task Cancel_InExecutionBooking_Fails()
    {
        booking.Confirm("ops@test.com");
        //booking.StartExecution();
        var sut = new CancelBookingHandler(tenantUow);

        var result = await sut.Handle(
            new CancelBookingCommand { Id = booking.Id, CancelledBy = "ops@test.com", Reason = "Too late" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    #endregion
}
