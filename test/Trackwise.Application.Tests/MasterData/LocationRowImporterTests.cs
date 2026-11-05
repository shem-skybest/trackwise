using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Location's importer resolves two lookups rather than one - the branch that maintains the record
/// and the zone it sits inside - and a zone named in a file has to be a zone, not just any location
/// that happens to carry the code.
/// </summary>
public class LocationRowImporterTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();

    private readonly ITenantRepository<Branch, Guid> branches =
        Substitute.For<ITenantRepository<Branch, Guid>>();

    private readonly ITenantRepository<Location, Guid> locations =
        Substitute.For<ITenantRepository<Location, Guid>>();

    private readonly ITenantRepository<Customer, Guid> customers =
        Substitute.For<ITenantRepository<Customer, Guid>>();

    private readonly Guid southZoneId = Guid.NewGuid();

    private readonly Guid hmiId = Guid.NewGuid();

    public LocationRowImporterTests()
    {
        tenantUow.Repository<Branch>().Returns(branches);
        tenantUow.Repository<Location>().Returns(locations);
        tenantUow.Repository<Customer>().Returns(customers);

        customers.Query().Returns(_ => new List<Customer>
        {
            new() { Id = hmiId, Name = "Hacienda Verde Inc." }
        }.BuildMock());

        branches.Query().Returns(_ => new List<Branch>
        {
            new() { Id = Guid.NewGuid(), Code = "MNL", Name = "Manila Head Office" }
        }.BuildMock());

        locations.Query().Returns(_ => new List<Location>
        {
            new()
            {
                Id = southZoneId,
                Code = "ZONE-S",
                Name = "South Luzon",
                Type = LocationType.Zone
            },

            // Same shape, wrong kind. A file naming this as a parent is the mistake the resolver
            // has to catch rather than accept.
            new()
            {
                Id = Guid.NewGuid(),
                Code = "WH-01",
                Name = "Rosario Warehouse",
                Type = LocationType.Warehouse
            }
        }.BuildMock());
    }

    private static MasterDataImportRow Row(params (string Column, string Value)[] values) =>
        new(2, values.ToDictionary(
            v => MasterDataImportHeader.Normalize(v.Column),
            v => (string?)v.Value));

    private static MasterDataImportRow ValidRow(params (string Column, string Value)[] overrides)
    {
        var values = new (string, string)[]
        {
            ("Name", "Cavite Export Processing Zone"),
            ("Type", "DeliveryPoint"),
            ("Street", "Gov. Drive"),
            ("City", "Rosario"),
            ("Province", "Cavite"),
            ("Region", "CALABARZON"),
            ("Zipcode", "4106")
        };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private Task<MasterDataRowBinding> Bind(MasterDataImportRow row) =>
        new LocationRowImporter(tenantUow).BindAsync(row, MasterDataImportOverride.None);

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow(("Zone", "zone-s"), ("Branch", "mnl")));

        var command = Assert.IsType<CreateLocationCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal(LocationType.DeliveryPoint, command.Type);
        Assert.Equal(southZoneId, command.ParentLocationId);
        Assert.Equal("Rosario", command.Address!.CityOrMunicipality);
    }

    [Fact]
    public async Task BindsTheOutletColumns()
    {
        var binding = await Bind(ValidRow(
            ("Customer", "hacienda verde inc."), ("Cutoff", "9:30"), ("ZoneRadius", "150")));

        var command = Assert.IsType<CreateLocationCommand>(binding.Command);
        Assert.Equal(hmiId, command.CustomerId);
        Assert.Equal(new TimeOnly(9, 30), command.DefaultCutoffTime);
        Assert.Equal(150, command.ZoneRadiusMeters);
    }

    [Fact]
    public async Task UnknownCustomer_IsRejectedRatherThanImportedAsShared()
    {
        // Filed as shared, the outlet would be offered on every client's loads.
        var binding = await Bind(ValidRow(("Customer", "HFI")));

        Assert.Null(binding.Command);
        Assert.Equal("Customer", Assert.Single(binding.Issues).Column);
    }

    [Fact]
    public async Task ACutoffThatIsNotATimeIsRejected()
    {
        var binding = await Bind(ValidRow(("Cutoff", "noon")));

        Assert.Null(binding.Command);
        Assert.Equal("Cutoff", Assert.Single(binding.Issues).Column);
    }

    [Fact]
    public async Task ABlankCountryMeansPhilippines()
    {
        // The legacy delivery-location table is entirely domestic and carries no country column at
        // all, so blank has to mean something rather than import as blank.
        var binding = await Bind(ValidRow());

        Assert.Equal("PH", ((CreateLocationCommand)binding.Command!).Address!.Country);
    }

    [Fact]
    public async Task AZoneRowCarriesNoAddress()
    {
        // Building an empty Address for a zone would trip the validator's "a zone has no address"
        // rule on every zone row in the file.
        var binding = await Bind(ValidRow(("Type", "Zone")));

        Assert.Null(((CreateLocationCommand)binding.Command!).Address);
    }

    [Fact]
    public async Task ALocationThatIsNotAZoneCannotBeNamedAsTheParent()
    {
        var binding = await Bind(ValidRow(("Zone", "WH-01")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Zone", issue.Column);
    }

    [Fact]
    public async Task UnknownZone_IsRejectedRatherThanImportedUnfiled()
    {
        var binding = await Bind(ValidRow(("Zone", "ZONE-X")));

        Assert.Null(binding.Command);
        Assert.Equal("Zone", Assert.Single(binding.Issues).Column);
    }

    [Fact]
    public async Task ACoordinateThatIsNotANumberIsRejectedRatherThanDropped()
    {
        // A delivery point that silently lost its coordinates looks identical to one that never
        // had any.
        var binding = await Bind(ValidRow(("Latitude", "fourteen")));

        Assert.Null(binding.Command);
        Assert.Equal("Latitude", Assert.Single(binding.Issues).Column);
    }

    [Fact]
    public async Task CoordinatesAreParsedWhenTheyAreNumbers()
    {
        var binding = await Bind(ValidRow(("Latitude", "14.4167"), ("Longitude", "120.85")));

        var command = (CreateLocationCommand)binding.Command!;
        Assert.Equal(14.4167, command.Latitude);
        Assert.Equal(120.85, command.Longitude);
    }
}
