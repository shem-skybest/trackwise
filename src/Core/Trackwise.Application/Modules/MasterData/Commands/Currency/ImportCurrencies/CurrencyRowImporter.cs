using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a currency out of one file row.
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CurrencyValidatorBase{T}"/>, so a file and the steward's form agree about what a
/// legal currency is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class CurrencyRowImporter : IMasterDataRowImporter
{
    public string EntityType => nameof(Currency);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        // The only required code column in the module's importers. Every other entity allocates one
        // when the cell is blank; a currency's is ISO 4217's to assign, so a blank is an error.
        new("Code", true, "ISO 4217 alpha-3 code.", "PHP"),
        new("Name", true, "Currency name as it should appear everywhere.", "Philippine Peso"),
        new("NumericCode", false, "ISO 4217 numeric code, three digits.", "608"),
        new("Symbol", false, "Display symbol.", "P"),
        new("MinorUnits", false, "Decimal places. Blank means 2.", "2"),
        new("Notes", false, "Anything a steward should know about this currency.", "")
    ];

    public Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        var (minorUnits, minorUnitsIssue) = ReadMinorUnits(row);

        if (minorUnitsIssue is not null)
        {
            return Task.FromResult(MasterDataRowBinding.Rejected(minorUnitsIssue));
        }

        return Task.FromResult(MasterDataRowBinding.For(new CreateCurrencyCommand
        {
            Code = row["Code"] ?? "",
            Name = row["Name"] ?? "",
            NumericCode = row["NumericCode"],
            Symbol = row["Symbol"],
            MinorUnits = minorUnits,
            Notes = row["Notes"],
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        }));
    }

    /// <summary>
    /// Reads the minor-units column, defaulting to two. Rejected rather than defaulted when it is
    /// unparseable: silently importing a yen amount as two-decimal money misstates every total in
    /// it by a factor of a hundred.
    /// </summary>
    private static (int Value, MasterDataImportIssue? Issue) ReadMinorUnits(MasterDataImportRow row)
    {
        var raw = row["MinorUnits"]?.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            return (2, null);
        }

        return int.TryParse(raw, out var value)
            ? (value, null)
            : (2, MasterDataImportIssue.Field(
                row.Line, "MinorUnits", $"'{raw}' is not a whole number."));
    }
}
