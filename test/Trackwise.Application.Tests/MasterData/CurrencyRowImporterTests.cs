using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Application.Modules.MasterData.Commands;
using Xunit;

namespace Trackwise.Application.Tests.MasterData;

/// <summary>
/// The currency importer is the only one in the module whose <c>Code</c> column is required - every
/// other entity allocates a code when the cell is blank, and a currency's is ISO 4217's to assign.
/// </summary>
public class CurrencyRowImporterTests
{
    private static MasterDataImportRow Row(params (string Column, string Value)[] values) =>
        new(2, values.ToDictionary(
            v => MasterDataImportHeader.Normalize(v.Column),
            v => (string?)v.Value));

    private static MasterDataImportRow ValidRow(params (string Column, string Value)[] overrides)
    {
        var values = new (string, string)[]
        {
            ("Code", "PHP"),
            ("Name", "Philippine Peso")
        };

        return Row([.. values.Where(v => overrides.All(o => o.Column != v.Item1)), .. overrides]);
    }

    private static Task<MasterDataRowBinding> Bind(MasterDataImportRow row) =>
        new CurrencyRowImporter().BindAsync(row, MasterDataImportOverride.None);

    [Fact]
    public void TheCodeColumnIsDeclaredRequired()
    {
        // The one importer in the module where it is. A blank cell here cannot be answered with an
        // allocated code, so the column declaration is what makes the file refuse it up front
        // rather than the validator refusing it a stage later.
        var code = Assert.Single(
            new CurrencyRowImporter().Columns, c => c.Name == "Code");

        Assert.True(code.Required);
    }

    [Fact]
    public async Task MinorUnitsDefaultToTwoWhenTheColumnIsBlank()
    {
        var binding = await Bind(ValidRow(("MinorUnits", "")));

        Assert.Equal(2, ((CreateCurrencyCommand)binding.Command!).MinorUnits);
    }

    [Fact]
    public async Task MinorUnitsAreReadWhenGiven()
    {
        // The yen is the reason this column exists at all.
        var binding = await Bind(ValidRow(("Code", "JPY"), ("Name", "Yen"), ("MinorUnits", "0")));

        Assert.Equal(0, ((CreateCurrencyCommand)binding.Command!).MinorUnits);
    }

    [Fact]
    public async Task MinorUnitsThatAreNotANumberAreRejectedRatherThanDefaulted()
    {
        // Quietly falling back to two would misstate every yen total by a factor of a hundred, and
        // the import report would say the row succeeded.
        var binding = await Bind(ValidRow(("MinorUnits", "two")));

        Assert.Null(binding.Command);
        var issue = Assert.Single(binding.Issues);
        Assert.Equal("MinorUnits", issue.Column);
        Assert.Equal(2, issue.Line);
    }

    [Fact]
    public async Task BindsAFullRowToACreateCommand()
    {
        var binding = await Bind(ValidRow(
            ("NumericCode", "608"),
            ("Symbol", "P"),
            ("MinorUnits", "2"),
            ("Notes", "Domestic default")));

        var command = Assert.IsType<CreateCurrencyCommand>(binding.Command);
        Assert.Empty(binding.Issues);
        Assert.Equal("PHP", command.Code);
        Assert.Equal("Philippine Peso", command.Name);
        Assert.Equal("608", command.NumericCode);
        Assert.Equal("P", command.Symbol);
    }
}
