using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using MockQueryable;
using NSubstitute;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// Commodity's importer adds the yes/no column, which is where a spreadsheet import is most likely
/// to lie quietly: a hazard flag that failed to parse and defaulted to false imports dangerous
/// goods as ordinary freight, and nothing in the report would say so.
/// </summary>
public class CommodityRowImporterTests
{
    private readonly ITenantUnitOfWork tenantUow = Substitute.For<ITenantUnitOfWork>();
    private readonly ITenantRepository<Branch, Guid> branches =
        Substitute.For<ITenantRepository<Branch, Guid>>();

    public CommodityRowImporterTests()
    {
        tenantUow.Repository<Branch>().Returns(branches);
        branches.Query().Returns(_ => new List<Branch>
        {
            new() { Id = Guid.NewGuid(), Code = "MNL", Name = "Manila Head Office" }
        }.BuildMock());
    }

    private static MasterDataImportRow Row(params (string Column, string Value)[] values) =>
        new(2, values.ToDictionary(
            v => MasterDataImportHeader.Normalize(v.Column),
            v => (string?)v.Value));

    private static MasterDataImportRow ValidRow(params (string Column, string Value)[] overrides)
    {
        var values = new (string, string)[] { ("Name", "Frozen Fish") };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private Task<MasterDataRowBinding> Bind(MasterDataImportRow row) =>
        new CommodityRowImporter(tenantUow).BindAsync(row, MasterDataImportOverride.None);

    [Theory]
    [InlineData("yes")]
    [InlineData("Y")]
    [InlineData("TRUE")]
    [InlineData("1")]
    public async Task AFlagIsReadFromEverySpellingASpreadsheetUses(string value)
    {
        var binding = await Bind(ValidRow(("Hazardous", value)));

        Assert.True(((CreateCommodityCommand)binding.Command!).IsHazardous);
    }

    [Theory]
    [InlineData("no")]
    [InlineData("N")]
    [InlineData("false")]
    [InlineData("0")]
    public async Task ANegativeFlagIsReadTheSameWay(string value)
    {
        var binding = await Bind(ValidRow(("Hazardous", value)));

        Assert.False(((CreateCommodityCommand)binding.Command!).IsHazardous);
    }

    [Fact]
    public async Task ABlankFlagMeansNo()
    {
        var binding = await Bind(ValidRow(("Hazardous", "")));

        Assert.False(((CreateCommodityCommand)binding.Command!).IsHazardous);
    }

    [Fact]
    public async Task AFlagThatIsNeitherIsRejectedRatherThanTreatedAsNo()
    {
        // The failure this exists to stop: "maybe" silently importing as ordinary freight.
        var binding = await Bind(ValidRow(("Hazardous", "maybe")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Hazardous", issue.Column);
        Assert.Equal(2, issue.Line);
    }

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow(
            ("HsCode", "0303.89"),
            ("Hazardous", "no"),
            ("TemperatureControlled", "yes"),
            ("Branch", "mnl")));

        var command = Assert.IsType<CreateCommodityCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal("Frozen Fish", command.Name);
        Assert.Equal("0303.89", command.HsCode);
        Assert.True(command.RequiresTemperatureControl);
        Assert.NotNull(command.BranchId);
    }
}
