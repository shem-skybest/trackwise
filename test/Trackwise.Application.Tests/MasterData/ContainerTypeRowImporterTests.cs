using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Commands;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The container-type importer is the first one that has to read a <i>category</i> out of a
/// spreadsheet, and that is where it can lie quietly: a category that failed to parse and defaulted
/// to general purpose imports a reefer as a dry box, and nothing in the report would say so.
/// </summary>
public class ContainerTypeRowImporterTests
{
    private static MasterDataImportRow Row(params (string Column, string Value)[] values) =>
        new(2, values.ToDictionary(
            v => MasterDataImportHeader.Normalize(v.Column),
            v => (string?)v.Value));

    private static MasterDataImportRow ValidRow(params (string Column, string Value)[] overrides)
    {
        var values = new (string, string)[]
        {
            ("Name", "40' High Cube"),
            ("Category", "General Purpose")
        };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private static Task<MasterDataRowBinding> Bind(MasterDataImportRow row) =>
        new ContainerTypeRowImporter().BindAsync(row, MasterDataImportOverride.None);

    [Theory]
    [InlineData("General Purpose")]
    [InlineData("general purpose")]
    [InlineData("GeneralPurpose")]
    [InlineData("GP")]
    [InlineData("Dry Van")]
    public async Task ACategoryIsReadFromEverySpellingAYardUses(string value)
    {
        var binding = await Bind(ValidRow(("Category", value)));

        Assert.Equal(
            ContainerTypeCategory.GeneralPurpose,
            ((CreateContainerTypeCommand)binding.Command!).Category);
    }

    [Theory]
    [InlineData("Reefer", ContainerTypeCategory.Reefer)]
    [InlineData("RF", ContainerTypeCategory.Reefer)]
    [InlineData("Open Top", ContainerTypeCategory.OpenTop)]
    [InlineData("Flat Rack", ContainerTypeCategory.FlatRack)]
    [InlineData("Tank", ContainerTypeCategory.Tank)]
    [InlineData("Bulk", ContainerTypeCategory.Bulk)]
    public async Task EveryCategoryIsReachableFromAFile(string value, ContainerTypeCategory expected)
    {
        var binding = await Bind(ValidRow(("Category", value)));

        Assert.Equal(expected, ((CreateContainerTypeCommand)binding.Command!).Category);
    }

    [Fact]
    public async Task ACategoryThatIsNoneOfThemIsRejectedRatherThanDefaulted()
    {
        // The failure this exists to stop: an unrecognised category silently importing as a dry
        // box, which is how a reefer load ends up planned into unrefrigerated equipment.
        var binding = await Bind(ValidRow(("Category", "chiller")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("Category", issue.Column);
        Assert.Equal(2, issue.Line);
    }

    [Fact]
    public async Task ABlankCategoryIsRejected()
    {
        // Unlike every optional column, a blank here has no sensible default at all - the enum's
        // zero value is general purpose, and defaulting to it would be the same silent lie.
        var binding = await Bind(ValidRow(("Category", "")));

        Assert.Null(binding.Command);
        Assert.Single(binding.Issues);
    }

    [Theory]
    [InlineData("40", 40)]
    [InlineData("40'", 40)]
    [InlineData("40 ft", 40)]
    public async Task ALengthIsReadThroughThePunctuationLegacyListsWriteItWith(
        string value, int expected)
    {
        // A file rejected over an apostrophe gets edited by hand rather than fixed at source.
        var binding = await Bind(ValidRow(("LengthFeet", value)));

        Assert.Equal(expected, ((CreateContainerTypeCommand)binding.Command!).LengthFeet);
    }

    [Fact]
    public async Task ALengthThatIsNotANumberIsRejected()
    {
        var binding = await Bind(ValidRow(("LengthFeet", "forty")));

        Assert.Null(binding.Command);
        Assert.Equal("LengthFeet", Assert.Single(binding.Issues).Column);
    }

    [Fact]
    public async Task ATeuFactorIsReadAsADecimal()
    {
        var binding = await Bind(ValidRow(("TeuFactor", "2.25")));

        Assert.Equal(2.25m, ((CreateContainerTypeCommand)binding.Command!).TeuFactor);
    }

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow(
            ("Code", "HC40"),
            ("IsoCode", "45G1"),
            ("LengthFeet", "40"),
            ("HighCube", "yes"),
            ("TeuFactor", "2")));

        var command = Assert.IsType<CreateContainerTypeCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal("HC40", command.Code);
        Assert.Equal("40' High Cube", command.Name);
        Assert.Equal("45G1", command.IsoSizeTypeCode);
        Assert.Equal(40, command.LengthFeet);
        Assert.True(command.IsHighCube);
        Assert.Equal(2m, command.TeuFactor);
    }
}
