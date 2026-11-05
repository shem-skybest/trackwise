using System.Linq.Expressions;
using Trackwise.Application.Modules.Integrations.LoadBoard.Queries;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Operations;

/// <summary>
/// A posted-load-board entry holds a non-nullable <c>TruckId</c> and used to flatten
/// <c>PostedTruck.Truck.Number</c> straight off the lazy navigation - a crash risk the moment a
/// referenced truck is soft-deleted, since the global query filter makes the navigation resolve to
/// null. These pin the fix: the handler batches the lookup through <c>TruckRefResolver</c>, which
/// reads past the filter, instead of dereferencing the navigation.
/// </summary>
public class TruckDisplayLookupTests
{
    private readonly ITenantRepository<PostedTruck, Guid> postedTruckRepo =
        Substitute.For<ITenantRepository<PostedTruck, Guid>>();

    private readonly ITenantRepository<Truck, Guid> truckRepo =
        Substitute.For<ITenantRepository<Truck, Guid>>();

    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();

    private readonly Truck truck = new()
    {
        Number = "TRK-042",
        Name = "2022 Freightliner Cascadia",
        Type = TruckType.FreightTruck
    };

    private readonly Guid unknownTruckId = Guid.NewGuid();

    private static Address SomeAddress() => new()
    {
        StreetAddress = "1 Logistics Way",
        CityOrMunicipality = "Manila",
        Province = "Metro Manila",
        Region = "NCR",
        Zipcode = "1000",
        Country = "PH"
    };

    private static GeoPoint SomeLocation() => new(120.9842, 14.5995);

    public TruckDisplayLookupTests()
    {
        tenantUow.Repository<PostedTruck>().Returns(postedTruckRepo);
        tenantUow.Repository<Truck>().Returns(truckRepo);

        postedTruckRepo.GetListAsync(
                Arg.Any<Expression<Func<PostedTruck, bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(
            [
                new PostedTruck
                {
                    TruckId = truck.Id,
                    // Deliberately null despite the compile-time non-nullable navigation: this is
                    // exactly what EF hands back once the referenced truck is soft-deleted and the
                    // global query filter hides it from a lazy load. A handler that still reads
                    // p.Truck.Number crashes here; the fixed one never touches this property.
                    Truck = null!,
                    ProviderType = LoadBoardProviderType.Dat,
                    AvailableAtAddress = SomeAddress(),
                    AvailableAtLocation = SomeLocation(),
                    AvailableFrom = DateTime.UtcNow
                },
                new PostedTruck
                {
                    // No truck resolves for this one either - the record must not throw, it must
                    // simply leave the display field unset.
                    TruckId = unknownTruckId,
                    Truck = null!,
                    ProviderType = LoadBoardProviderType.Dat,
                    AvailableAtAddress = SomeAddress(),
                    AvailableAtLocation = SomeLocation(),
                    AvailableFrom = DateTime.UtcNow
                }
            ]);

        // Reached through IgnoreQueryFilters() inside TruckRefResolver - the resolver must still
        // find a truck even after it has been soft-deleted, which is what makes this safe to read
        // from a page that shows retired trucks' history.
        truckRepo.Query().Returns(new List<Truck> { truck }.BuildMock());
    }

    [Fact]
    public async Task PostedTruckList_ResolvesTruckNumberWithoutTheNavigationProperty()
    {
        var sut = new GetPostedTrucksHandler(tenantUow);

        var result = await sut.Handle(new GetPostedTrucksQuery(), default);

        var resolved = result.Value!.Single(p => p.TruckId == truck.Id);

        Assert.Equal("TRK-042", resolved.TruckNumber);
    }

    [Fact]
    public async Task PostedTruckList_LooksTrucksUpOnceForTheWholePage()
    {
        var sut = new GetPostedTrucksHandler(tenantUow);

        await sut.Handle(new GetPostedTrucksQuery(), default);

        // One query for the page, not one per row.
        truckRepo.Received(1).Query();
    }

    [Fact]
    public async Task PostedTruckWithNoResolvableTruck_LeavesTheDisplayValueUnsetInsteadOfThrowing()
    {
        var sut = new GetPostedTrucksHandler(tenantUow);

        var result = await sut.Handle(new GetPostedTrucksQuery(), default);

        var unresolved = result.Value!.Single(p => p.TruckId == unknownTruckId);

        Assert.Null(unresolved.TruckNumber);
    }
}
