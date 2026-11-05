using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a container type out of one file row.
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="ContainerTypeValidatorBase{T}"/>, so a file and the steward's form agree about what a
/// legal container type is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class ContainerTypeRowImporter : IMasterDataRowImporter
{
    public string EntityType => nameof(ContainerType);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Code", false, "Yard shorthand. Leave blank to have one allocated.", "HC40"),
        new("Name", true, "Type name as it should appear everywhere.", "40' High Cube"),
        new("IsoCode", false, "ISO 6346 size-type code, four characters.", "45G1"),
        new("Category", true, "General Purpose, Reefer, Open Top, Flat Rack, Tank or Bulk.", "General Purpose"),
        new("LengthFeet", false, "10, 20, 40, 45 or 53. Blank for equipment with no standard length.", "40"),
        new("HighCube", false, "Yes or no. Blank means no.", "yes"),
        new("TeuFactor", false, "TEU equivalent - 1 for a 20', 2 for a 40', 2.25 for a 45'.", "2"),
        new("Notes", false, "Anything a steward should know about this type.", "")
    ];

    public Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        var (category, categoryIssue) = ReadCategory(row);

        if (categoryIssue is not null)
        {
            return Task.FromResult(MasterDataRowBinding.Rejected(categoryIssue));
        }

        var (highCube, highCubeIssue) = ReadFlag(row, "HighCube");

        if (highCubeIssue is not null)
        {
            return Task.FromResult(MasterDataRowBinding.Rejected(highCubeIssue));
        }

        var (lengthFeet, lengthIssue) = ReadInt(row, "LengthFeet");

        if (lengthIssue is not null)
        {
            return Task.FromResult(MasterDataRowBinding.Rejected(lengthIssue));
        }

        var (teuFactor, teuIssue) = ReadDecimal(row, "TeuFactor");

        if (teuIssue is not null)
        {
            return Task.FromResult(MasterDataRowBinding.Rejected(teuIssue));
        }

        return Task.FromResult(MasterDataRowBinding.For(new CreateContainerTypeCommand
        {
            Code = row["Code"],
            Name = row["Name"] ?? "",
            IsoSizeTypeCode = row["IsoCode"],
            Category = category,
            LengthFeet = lengthFeet,
            IsHighCube = highCube,
            TeuFactor = teuFactor,
            Notes = row["Notes"],
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        }));
    }

    /// <summary>
    /// Reads the category column. Accepted as a steward writes it - "General Purpose", "general
    /// purpose", "GeneralPurpose" and "GP" all land on the same member - because the alternative is
    /// a file rejected over a space. An unrecognised value is named rather than defaulted: silently
    /// importing an unknown category as general purpose would put a reefer in a dry box.
    /// </summary>
    private static (ContainerTypeCategory Value, MasterDataImportIssue? Issue) ReadCategory(
        MasterDataImportRow row)
    {
        var raw = row["Category"]?.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            return (default, MasterDataImportIssue.Field(
                row.Line, "Category", "A container type needs a category."));
        }

        var collapsed = new string([.. raw.Where(char.IsAsciiLetter)]).ToLowerInvariant();

        return collapsed switch
        {
            "generalpurpose" or "gp" or "dry" or "dryvan" => (ContainerTypeCategory.GeneralPurpose, null),
            "reefer" or "rf" or "refrigerated" => (ContainerTypeCategory.Reefer, null),
            "opentop" or "ot" => (ContainerTypeCategory.OpenTop, null),
            "flatrack" or "fr" or "flat" or "platform" => (ContainerTypeCategory.FlatRack, null),
            "tank" or "tanker" or "tk" => (ContainerTypeCategory.Tank, null),
            "bulk" => (ContainerTypeCategory.Bulk, null),
            _ => (default, MasterDataImportIssue.Field(
                row.Line,
                "Category",
                $"'{raw}' is not a category. Use General Purpose, Reefer, Open Top, Flat Rack, Tank or Bulk."))
        };
    }

    /// <summary>
    /// Reads a yes/no column. Spreadsheets spell a boolean six ways, so all six are accepted and
    /// anything else is rejected by name.
    /// </summary>
    private static (bool Value, MasterDataImportIssue? Issue) ReadFlag(
        MasterDataImportRow row, string column)
    {
        var raw = row[column]?.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            return (false, null);
        }

        return raw.ToLowerInvariant() switch
        {
            "yes" or "y" or "true" or "1" => (true, null),
            "no" or "n" or "false" or "0" => (false, null),
            _ => (false, MasterDataImportIssue.Field(
                row.Line, column, $"'{raw}' is not a yes or no."))
        };
    }

    private static (int? Value, MasterDataImportIssue? Issue) ReadInt(
        MasterDataImportRow row, string column)
    {
        var raw = row[column]?.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            return (null, null);
        }

        // Legacy lists write the length as "40'" and "40 ft", and a file rejected over the
        // punctuation would be edited by hand rather than fixed at source.
        var digits = new string([.. raw.Where(char.IsAsciiDigit)]);

        return int.TryParse(digits, out var value)
            ? (value, null)
            : (null, MasterDataImportIssue.Field(row.Line, column, $"'{raw}' is not a whole number."));
    }

    private static (decimal? Value, MasterDataImportIssue? Issue) ReadDecimal(
        MasterDataImportRow row, string column)
    {
        var raw = row[column]?.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            return (null, null);
        }

        return decimal.TryParse(
            raw, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? (value, null)
            : (null, MasterDataImportIssue.Field(row.Line, column, $"'{raw}' is not a number."));
    }
}
