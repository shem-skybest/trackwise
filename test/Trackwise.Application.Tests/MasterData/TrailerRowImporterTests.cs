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
/// Trailer's importer adds three numeric columns to the port shape. A value that fails to parse is
/// rejected by name rather than dropped, because a trailer that silently lost its axle count looks
/// identical to one that never had it recorded.
/// </summary>
public class TrailerRowImporterTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Branch, Guid> branches =
        Substitute.For<ITenantRepository<Branch, Guid>>();

    private readonly Guid manilaId = Guid.NewGuid();

    public TrailerRowImporterTests()
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
            ("Name", "53' Dry Van #12"),
            ("Type", "DryVan")
        };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private Task<MasterDataRowBinding> Bind(MasterDataImportRow row) =>
        new TrailerRowImporter(tenantUow).BindAsync(row, MasterDataImportOverride.None);

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow(
            ("Vin", "1GRAA0620XB123456"), ("AxleCount", "2"), ("MaxLoadCapacityKg", "28000")));

        var command = Assert.IsType<CreateTrailerCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal("53' Dry Van #12", command.Name);
        Assert.Equal(TrailerType.DryVan, command.Type);
        Assert.Equal("1GRAA0620XB123456", command.Vin);
        Assert.Equal(2, command.AxleCount);
        Assert.Equal(28000, command.MaxLoadCapacityKg);
    }

    [Fact]
    public async Task ABlankAxleCountMeansUnknownRatherThanZero()
    {
        var binding = await Bind(ValidRow(("AxleCount", "")));

        Assert.Null(((CreateTrailerCommand)binding.Command!).AxleCount);
    }

    [Fact]
    public async Task AnAxleCountThatIsNotANumberIsRejectedAgainstItsColumn()
    {
        var binding = await Bind(ValidRow(("AxleCount", "a couple")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("AxleCount", issue.Column);
        Assert.Equal(2, issue.Line);
    }

    [Fact]
    public async Task ACapacityThatIsNotANumberIsRejectedAgainstItsColumn()
    {
        var binding = await Bind(ValidRow(("MaxLoadCapacityKg", "about 28 tons")));

        Assert.Null(binding.Command);
        Assert.Equal("MaxLoadCapacityKg", Assert.Single(binding.Issues).Column);
    }

    [Fact]
    public async Task UnknownType_IsRejectedAgainstItsColumnAndListsTheOptions()
    {
        var binding = await Bind(ValidRow(("Type", "Container")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Type", issue.Column);
        Assert.Contains(nameof(TrailerType.ContainerChassis), issue.Message);
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
