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
/// Vessel's importer adds one thing to the port shape: a numeric column. A capacity that fails to
/// parse is rejected by name rather than dropped, because a ship that silently lost its TEU figure
/// looks identical to a tanker that never had one.
/// </summary>
public class VesselRowImporterTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Branch, Guid> branches =
        Substitute.For<ITenantRepository<Branch, Guid>>();

    private readonly Guid manilaId = Guid.NewGuid();

    public VesselRowImporterTests()
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
        var values = new (string, string)[]
        {
            ("Name", "Ever Given"),
            ("IMO", "9811000"),
            ("Type", "ContainerShip")
        };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private Task<MasterDataRowBinding> Bind(MasterDataImportRow row) =>
        new VesselRowImporter(tenantUow).BindAsync(row, MasterDataImportOverride.None);

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow(("TeuCapacity", "20124"), ("Flag", "pa")));

        var command = Assert.IsType<CreateVesselCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal("Ever Given", command.Name);
        Assert.Equal(VesselType.ContainerShip, command.Type);
        Assert.Equal(20124, command.TeuCapacity);
        Assert.Equal("PA", command.FlagCountryCode);
    }

    [Fact]
    public async Task ABlankCapacityMeansUnknownRatherThanZero()
    {
        var binding = await Bind(ValidRow(("TeuCapacity", "")));

        Assert.Null(((CreateVesselCommand)binding.Command!).TeuCapacity);
    }

    [Fact]
    public async Task ACapacityThatIsNotANumberIsRejectedAgainstItsColumn()
    {
        var binding = await Bind(ValidRow(("TeuCapacity", "about 20k")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("TeuCapacity", issue.Column);
        Assert.Equal(2, issue.Line);
    }

    [Fact]
    public async Task UnknownType_IsRejectedAgainstItsColumnAndListsTheOptions()
    {
        var binding = await Bind(ValidRow(("Type", "Steamship")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Type", issue.Column);
        Assert.Contains(nameof(VesselType.BulkCarrier), issue.Message);
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
