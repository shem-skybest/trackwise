using Trackwise.Application.Modules.Operations.Loads.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.Loads;

/// <summary>
/// A load may only point at its own customer's outlets or a shared site, and never at a zone -
/// otherwise the picker's customer filter is the only thing keeping one client's drops off another's.
/// </summary>
public class LoadServiceSiteRulesTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Location, Guid> locationRepo = Substitute.For<ITenantRepository<Location, Guid>>();

    private readonly Employee dispatcher = new() { Code = "DANADOE", Email = "dispatcher@test.com", FirstName = "Dana", LastName = "Doe" };
    private readonly Customer customer = new() { Name = "Hacienda Verde Inc." };
    private readonly List<Location> sites = [];

    private readonly LoadService sut;

    public LoadServiceSiteRulesTests()
    {
        var employeeRepo = Substitute.For<ITenantRepository<Employee, Guid>>();
        var truckRepo = Substitute.For<ITenantRepository<Truck, Guid>>();
        var customerRepo = Substitute.For<ITenantRepository<Customer, Guid>>();
        var tripRepo = Substitute.For<ITenantRepository<Trip, Guid>>();

        tenantUow.Repository<Employee>().Returns(employeeRepo);
        tenantUow.Repository<Truck>().Returns(truckRepo);
        tenantUow.Repository<Customer>().Returns(customerRepo);
        tenantUow.Repository<Trip>().Returns(tripRepo);
        tenantUow.Repository<Booking>().Returns(Substitute.For<ITenantRepository<Booking, Guid>>());
        tenantUow.Repository<Load>().Returns(Substitute.For<ITenantRepository<Load, Guid>>());
        tenantUow.Repository<Location>().Returns(locationRepo);

        employeeRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Employee, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([dispatcher]);
        truckRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Truck, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        customerRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([customer]);
        tripRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Trip, bool>>>(), Arg.Any<CancellationToken>())
            .Returns([]);
        locationRepo.GetListAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Location, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(_ => sites.ToList());

        sut = new LoadService(tenantUow);
    }

    private Location Site(Guid? customerId, LocationType type = LocationType.DeliveryPoint, bool isActive = true)
    {
        var site = new Location
        {
            Id = Guid.NewGuid(),
            Code = $"S-{sites.Count}",
            Name = $"Site {sites.Count}",
            Type = type,
            CustomerId = customerId,
            IsActive = isActive
        };
        sites.Add(site);
        return site;
    }

    private Task<Load> Create(Guid? destinationSiteId)
    {
        var address = new Address { StreetAddress = "Q. Baetiong Drive", CityOrMunicipality = "Quezon City", Province = "Metro Manila", Region = "NCR", Zipcode = "", Country = "PH" };
        return sut.CreateLoadAsync(new CreateLoadParameters(
            "Milk drop",
            LoadType.GeneralFreight,
            (address, new GeoPoint(121.0, 14.6)),
            (address, new GeoPoint(121.0, 14.65)),
            1000m,
            5000,
            customer.Id,
            null,
            dispatcher.Id,
            DestinationSiteId: destinationSiteId), ct: CancellationToken.None);
    }

    [Fact]
    public async Task TheCustomersOwnOutlet_IsLinked()
    {
        var outlet = Site(customer.Id);

        var load = await Create(outlet.Id);

        Assert.Equal(outlet.Id, load.DestinationSiteId);
    }

    [Fact]
    public async Task ASharedSite_IsLinked()
    {
        var shared = Site(null);

        var load = await Create(shared.Id);

        Assert.Equal(shared.Id, load.DestinationSiteId);
    }

    [Fact]
    public async Task AnotherCustomersOutlet_IsRefused()
    {
        var foreign = Site(Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Create(foreign.Id));
        Assert.Contains("another customer", ex.Message);
    }

    [Fact]
    public async Task AZone_IsRefused()
    {
        var zone = Site(null, LocationType.Zone);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(zone.Id));
    }

    [Fact]
    public async Task AnInactiveSite_IsRefusedForNewWork()
    {
        var closed = Site(customer.Id, isActive: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(closed.Id));
    }

    [Fact]
    public async Task AnUnknownSite_IsRefused()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(Guid.NewGuid()));
    }
}
