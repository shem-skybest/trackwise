using MockQueryable;
using NSubstitute;
using Trackwise.Application.Modules.Operations.FreightForwarding.Queries;
using Trackwise.Domain.Core;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.Enums.FreightForwarding;
using Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// The dispatcher's planning board: the day's Confirmed bookings that have no trip yet,
/// earliest requested pickup first.
/// </summary>
public class GetUnassignedBookingsHandlerTests
{
    private static readonly DateOnly Tomorrow = new(2026, 10, 6);

    // Must match GetUnassignedBookingsHandler.BusinessUtcOffset (UTC+8).
    private static readonly TimeSpan BusinessOffset = TimeSpan.FromHours(8);

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly Location warehouse = Site("HFI Warehouse");
    private readonly Location fairview = Site("SM Fairview");
    private readonly Location novaliches = Site("SM Novaliches");
    private readonly Customer hfi = new() { Name = "HFI" };
    private readonly List<Booking> bookings = [];
    private readonly GetUnassignedBookingsHandler sut;

    public GetUnassignedBookingsHandlerTests()
    {
        Stub<Booking>(() => bookings);
        Stub<Location>(() => [warehouse, fairview, novaliches]);
        Stub<Customer>(() => [hfi]);

        sut = new GetUnassignedBookingsHandler(tenantUow);
    }

    [Fact]
    public async Task Handle_ListsOnlyThatDaysConfirmedBookings_EarliestPickupFirst()
    {
        var late = Book("QT-2026-0002", At(Tomorrow, 5, 0), BookingStatus.Confirmed, fairview);
        var early = Book("QT-2026-0003", At(Tomorrow, 2, 0), BookingStatus.Confirmed, fairview, novaliches);

        Book("QT-2026-0001", At(Tomorrow, 1, 0), BookingStatus.Draft, fairview);          // not ready to plan
        Book("QT-2026-0004", At(Tomorrow, 3, 0), BookingStatus.InProgress, fairview);     // already has a trip
        Book("QT-2026-0005", At(Tomorrow, 3, 0), BookingStatus.Cancelled, fairview);
        Book("QT-2026-0006", At(Tomorrow.AddDays(1), 1, 0), BookingStatus.Confirmed, fairview); // another day

        var result = await sut.Handle(new GetUnassignedBookingsQuery { Date = Tomorrow }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            [early.BookingNumber, late.BookingNumber],
            result.Value!.Select(r => r.BookingNumber));
    }

    [Fact]
    public async Task Handle_ResolvesCustomerLocationNamesAndDrops()
    {
        var booking = Book("QT-2026-0003", At(Tomorrow, 2, 0), BookingStatus.Confirmed, fairview, novaliches);

        var result = await sut.Handle(new GetUnassignedBookingsQuery { Date = Tomorrow }, CancellationToken.None);

        var row = Assert.Single(result.Value!);
        Assert.Equal(booking.Id, row.Id);
        Assert.Equal("BK-2026-0003", row.BookingNumber);
        Assert.Equal("QT-2026-0003", row.QuotationReference);
        Assert.Equal("HFI", row.CustomerName);
        Assert.Equal("HFI Warehouse", row.OriginName);
        Assert.Equal("SM Novaliches", row.DestinationName);
        Assert.Equal(["SM Fairview", "SM Novaliches"], row.DropNames);
        Assert.Equal(3, row.StopCount);
        Assert.Equal("Vaccines", row.Commodity);
    }

    [Fact]
    public async Task Handle_SelectsTheBusinessDay_NotTheUtcDay()
    {
        // 00:30 local on the day is still the PREVIOUS day in UTC, so a UTC-day filter would drop it.
        var justAfterMidnight = Book("QT-2026-0010", At(Tomorrow, 0, 30), BookingStatus.Confirmed, fairview);
        var lateEvening = Book("QT-2026-0011", At(Tomorrow, 23, 30), BookingStatus.Confirmed, fairview);

        Book("QT-2026-0012", At(Tomorrow.AddDays(-1), 23, 30), BookingStatus.Confirmed, fairview); // day before
        Book("QT-2026-0013", At(Tomorrow.AddDays(1), 0, 30), BookingStatus.Confirmed, fairview);   // day after

        var result = await sut.Handle(new GetUnassignedBookingsQuery { Date = Tomorrow }, CancellationToken.None);

        Assert.Equal(
            [justAfterMidnight.BookingNumber, lateEvening.BookingNumber],
            result.Value!.Select(r => r.BookingNumber));
    }

    [Fact]
    public async Task Handle_NoBookingsThatDay_ReturnsEmptyList()
    {
        Book("QT-2026-0020", At(Tomorrow.AddDays(3), 2, 0), BookingStatus.Confirmed, fairview);

        var result = await sut.Handle(new GetUnassignedBookingsQuery { Date = Tomorrow }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static DateTimeOffset At(DateOnly date, int hour, int minute) =>
        new(date.ToDateTime(new TimeOnly(hour, minute)), BusinessOffset);

    /// <summary>
    /// Builds a booking directly in the given status. This test is about which bookings the query
    /// returns, not how they got there: reaching Confirmed through the domain needs an Accepted
    /// quotation with issued price snapshots, verified documents, and a credit check. Those
    /// transitions belong in the Booking domain tests.
    /// </summary>
    private Booking Book(string quotationReference, DateTimeOffset pickup, BookingStatus status, params Location[] drops)
    {
        var stops = new List<RouteStop> { RouteStop.Create(1, warehouse.Id, StopType.Pickup, pickup, null) };
        for (var i = 0; i < drops.Length; i++)
        {
            stops.Add(RouteStop.Create(
                i + 2, drops[i].Id, StopType.Dropoff, null,
                i == drops.Length - 1 ? pickup.AddHours(6) : null));
        }

        var booking = (Booking)Activator.CreateInstance(typeof(Booking), nonPublic: true)!;

        Set(booking, nameof(Booking.BookingNumber), BookingNumbering.FromQuotationReference(quotationReference));
        Set(booking, nameof(Booking.Status), status);
        Set(booking, nameof(Booking.CustomerId), hfi.Id);
        Set(booking, nameof(Booking.Currency), "PHP");
        Set(booking, nameof(Booking.QuotationId), Guid.NewGuid());
        Set(booking, nameof(Booking.QuotationReference), quotationReference);
        Set(booking, nameof(Booking.Mode), TransportMode.Road);
        Set(booking, nameof(Booking.FreightMode), FreightMode.Domestic);
        Set(booking, nameof(Booking.ShipmentType), ShipmentType.FTL);
        Set(booking, nameof(Booking.Lane), new LaneTransit(stops, 100m));
        Set(booking, nameof(Booking.CargoSpec), CargoSpecification.Create(
            "Vaccines", 1000m, 2, new Money { Amount = 5000m, Currency = "PHP" }));
        Set(booking, nameof(Booking.RequestedPickupStart), pickup);
        Set(booking, nameof(Booking.RequestedDeliveryEnd), pickup.AddHours(6));

        bookings.Add(booking);
        return booking;
    }

    private static void Set(Booking booking, string property, object? value) =>
        typeof(Booking).GetProperty(property)!.SetValue(booking, value);

    private void Stub<T>(Func<List<T>> rows) where T : class, IEntity<Guid>, ITenantEntity
    {
        var repo = Substitute.For<ITenantRepository<T, Guid>>();
        repo.Query().Returns(_ => rows().BuildMock());
        tenantUow.Repository<T>().Returns(repo);
    }

    private static Location Site(string name) => new()
    {
        Code = name.ToUpperInvariant().Replace(' ', '-'),
        Name = name,
        Type = LocationType.DeliveryPoint
    };
}
