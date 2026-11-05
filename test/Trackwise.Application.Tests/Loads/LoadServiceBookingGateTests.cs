using NSubstitute;
using Trackwise.Application.Modules.Operations.Loads.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.Loads;

public class LoadServiceBookingGateTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();

    private readonly ITenantRepository<Employee, Guid> employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
    private readonly ITenantRepository<Truck, Guid> truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
    private readonly ITenantRepository<Customer, Guid> customerRepo = Substitute.For<ITenantRepository<Customer, Guid>>();
    private readonly ITenantRepository<Trip, Guid> tripRepo = Substitute.For<ITenantRepository<Trip, Guid>>();
    private readonly ITenantRepository<Booking, Guid> bookingRepo = Substitute.For<ITenantRepository<Booking, Guid>>();
    private readonly ITenantRepository<Load, Guid> loadRepo = Substitute.For<ITenantRepository<Load, Guid>>();

    private readonly Employee dispatcher = new() { Code = "DANADOE", Email = "dispatcher@test.com", FirstName = "Dana", LastName = "Doe" };
    private readonly Customer customer = new() { Name = "Acme Co" };
    private readonly Booking booking;

    private readonly LoadService sut;

    public LoadServiceBookingGateTests()
    {
        var lane = LaneTransit.PointToPoint(
            originLocationId: Guid.NewGuid(),
            destinationLocationId: Guid.NewGuid(),
            pickupStart: DateTimeOffset.UtcNow,
            deliveryEnd: DateTimeOffset.UtcNow.AddDays(1),
            distanceKm: 150m);

        var cargo = CargoSpecification.Create(
            commodity: "General Merchandise",
            totalWeightKg: 1000m,
            palletCount: 2,
            declaredCargoValue: new Money { Amount = 10000m, Currency = "USD" },
            requiresActiveRefrigeration: false);

        var quotation = Quotation.Create(
            customerId: customer.Id,
            referenceNumber: "QT-TEST-001",
            currency: "USD",
            mode: TransportMode.Road,
            freightMode: FreightMode.Domestic,
            shipmentType: ShipmentType.FTL,
            incoterm: Incoterm.DAP,
            lane: lane,
            cargoSpec: cargo);

        quotation.Approve("manager@test.com");
        booking = Booking.CreateDraftFromQuotation(quotation);

        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<Customer>().Returns(customerRepo);
        tenantUow.Repository<Trip>().Returns(tripRepo);
        tenantUow.Repository<Booking>().Returns(bookingRepo);
        tenantUow.Repository<Load>().Returns(loadRepo);

        employeeRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Employee, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([dispatcher]);
        truckRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Truck, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        customerRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([customer]);
        tripRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Trip, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        bookingRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Booking, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([booking]);

        sut = new LoadService(tenantUow);
    }

    private CreateLoadParameters BuildParameters(Guid? bookingId)
    {
        var address = new Address { StreetAddress = "1 St", CityOrMunicipality = "Dallas", Province = "TX", Region = "", Zipcode = "75001", Country = "US" };
        return new CreateLoadParameters(
            "Test Load",
            LoadType.Vehicle,
            (address, new GeoPoint(-96.8, 32.8)),
            (address, new GeoPoint(-87.6, 41.9)),
            1000m,
            500,
            customer.Id,
            null,
            dispatcher.Id,
            BookingId: bookingId);
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed)]
    [InlineData(BookingStatus.InProgress)]
    public async Task CreateLoadAsync_BookingConfirmedOrInExecution_Succeeds(BookingStatus status)
    {
        if (status == BookingStatus.InProgress)
        {
            booking.Confirm("ops@test.com");
            //booking.StartExecution();
        }
        else
        {
            booking.Confirm("ops@test.com");
        }

        var load = await sut.CreateLoadAsync(BuildParameters(booking.Id), ct: CancellationToken.None);

        Assert.Equal(booking.Id, load.BookingId);
    }

    [Fact]
    public async Task CreateLoadAsync_PendingBooking_Throws()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateLoadAsync(BuildParameters(booking.Id), ct: CancellationToken.None));
    }

    [Fact]
    public async Task CreateLoadAsync_CancelledBooking_Throws()
    {
        booking.Cancel("ops@test.com", "Client backed out");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateLoadAsync(BuildParameters(booking.Id), ct: CancellationToken.None));
    }

    [Fact]
    public async Task CreateLoadAsync_NoBookingReferenced_Succeeds()
    {
        var load = await sut.CreateLoadAsync(BuildParameters(null), ct: CancellationToken.None);

        Assert.Null(load.BookingId);
    }
}
