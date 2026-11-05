using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Helper's importer has no type to parse and no second identifier - the only thing beyond a plain
/// name/branch shape is the optional address block, which stays empty rather than half-filled.
/// </summary>
public class HelperRowImporterTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Branch, Guid> branches =
        Substitute.For<ITenantRepository<Branch, Guid>>();

    private readonly Guid manilaId = Guid.NewGuid();

    public HelperRowImporterTests()
    {
        tenantUow.Repository<Branch>().Returns(branches);
        branches.Query().Returns(_ => new List<Branch>
        {
            new() { Id = manilaId, Code = "MNL", Name = "Manila Head Office" }
        }.BuildMock());
    }

    private static MasterDataImportRow Row(params (string Column, string Value)[] values) =>
        new(2, values.ToDictionary(
            v => MasterDataImportHeader.Normalize(v.Column),
            v => (string?)v.Value));

    private static MasterDataImportRow ValidRow(params (string Column, string Value)[] overrides)
    {
        var values = new (string, string)[] { ("Name", "Juan Dela Cruz") };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private Task<MasterDataRowBinding> Bind(MasterDataImportRow row) =>
        new HelperRowImporter(tenantUow).BindAsync(row, MasterDataImportOverride.None);

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow(("PhoneNumber", "+63 917 123 4567")));

        var command = Assert.IsType<CreateHelperCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal("Juan Dela Cruz", command.Name);
        Assert.Equal("+63 917 123 4567", command.PhoneNumber);
        Assert.Null(command.Address);
    }

    [Fact]
    public async Task NoAddressColumnsMeansNoAddress_RatherThanOneWithEveryFieldBlank()
    {
        var binding = await Bind(ValidRow());

        Assert.Null(((CreateHelperCommand)binding.Command!).Address);
    }

    [Fact]
    public async Task AnyAddressColumnBuildsTheWholeBlock()
    {
        var binding = await Bind(ValidRow(("City", "Manila"), ("Country", "ph")));

        var address = ((CreateHelperCommand)binding.Command!).Address;
        Assert.NotNull(address);
        Assert.Equal("Manila", address!.CityOrMunicipality);
        Assert.Equal("PH", address.Country);
        Assert.Equal("", address.StreetAddress);
    }

    [Fact]
    public async Task UnknownBranch_IsRejectedRatherThanImportedAsGlobal()
    {
        // "Applies everywhere" is a decision a steward makes, not a fallback for a typo.
        var binding = await Bind(ValidRow(("Branch", "XXX")));

        Assert.Null(binding.Command);
        Assert.Equal("Branch", Assert.Single(binding.Issues).Column);
    }
}
