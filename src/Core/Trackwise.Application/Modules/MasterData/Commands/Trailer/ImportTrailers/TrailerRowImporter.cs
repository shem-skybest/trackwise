using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a trailer out of one file row.
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CreateTrailerValidator"/>, so a file and the steward's form agree about what a legal
/// trailer is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class TrailerRowImporter(ITenantUnitOfWork tenantUow) : IMasterDataRowImporter
{
    /// <summary>
    /// Branch codes resolved so far. A file scoped to one office would otherwise re-query the same
    /// branch on every one of its rows.
    /// </summary>
    private readonly Dictionary<string, Guid?> branches = new(StringComparer.Ordinal);

    public string EntityType => nameof(Trailer);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Code", false, "Leave blank to have one allocated.", "TRL-0001"),
        new("Name", true, "Descriptive label as it should appear everywhere.", "53' Dry Van #12"),
        new("Type", true, $"One of: {string.Join(", ", Enum.GetNames<TrailerType>())}.", nameof(TrailerType.DryVan)),
        new("Vin", false, "17-character VIN, if known.", "1GRAA0620XB123456"),
        new("LicensePlate", false, "Plate number, if any.", "TRL-4521"),
        new("LicensePlateState", false, "State or province the plate is registered in.", "TX"),
        new("Make", false, "Manufacturer.", "Great Dane"),
        new("Model", false, "Model.", "Everest"),
        new("Year", false, "Year of manufacture.", "2019"),
        new("AxleCount", false, "Number of axles.", "2"),
        new("MaxLoadCapacityKg", false, "Maximum load capacity in kilograms.", "28000"),
        new("Branch", false, "Code of the branch that maintains it. Blank means every branch.", "MNL"),
        new("Notes", false, "Anything a steward should know about this trailer.", "")
    ];

    public async Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        if (!Enum.TryParse<TrailerType>(row["Type"], ignoreCase: true, out var type))
        {
            return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                row.Line,
                "Type",
                $"'{row["Type"]}' is not a trailer type. Use one of: {string.Join(", ", Enum.GetNames<TrailerType>())}."));
        }

        var year = (int?)null;

        if (!string.IsNullOrWhiteSpace(row["Year"]))
        {
            if (!int.TryParse(row["Year"], out var parsedYear))
            {
                return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                    row.Line, "Year", $"'{row["Year"]}' is not a whole number."));
            }

            year = parsedYear;
        }

        var axleCount = (int?)null;

        if (!string.IsNullOrWhiteSpace(row["AxleCount"]))
        {
            if (!int.TryParse(row["AxleCount"], out var parsedAxles))
            {
                return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                    row.Line, "AxleCount", $"'{row["AxleCount"]}' is not a whole number."));
            }

            axleCount = parsedAxles;
        }

        var maxLoadCapacityKg = (decimal?)null;

        if (!string.IsNullOrWhiteSpace(row["MaxLoadCapacityKg"]))
        {
            if (!decimal.TryParse(row["MaxLoadCapacityKg"], out var parsedCapacity))
            {
                return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                    row.Line, "MaxLoadCapacityKg", $"'{row["MaxLoadCapacityKg"]}' is not a number."));
            }

            maxLoadCapacityKg = parsedCapacity;
        }

        var (branchId, branchIssue) = await ResolveBranchAsync(row, ct);

        if (branchIssue is not null)
        {
            return MasterDataRowBinding.Rejected(branchIssue);
        }

        return MasterDataRowBinding.For(new CreateTrailerCommand
        {
            Code = row["Code"],
            Name = row["Name"] ?? "",
            Type = type,
            Vin = row["Vin"],
            LicensePlate = row["LicensePlate"],
            LicensePlateState = row["LicensePlateState"],
            Make = row["Make"],
            Model = row["Model"],
            Year = year,
            AxleCount = axleCount,
            MaxLoadCapacityKg = maxLoadCapacityKg,
            BranchId = branchId,
            Notes = row["Notes"],
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        });
    }

    /// <summary>
    /// Turns a branch code into its id. A file names the office the way a person does; an unknown
    /// code is rejected rather than quietly imported as a global trailer, because "applies
    /// everywhere" is a decision, not a fallback for a typo.
    /// </summary>
    private async Task<(Guid? BranchId, MasterDataImportIssue? Issue)> ResolveBranchAsync(
        MasterDataImportRow row, CancellationToken ct)
    {
        var code = Branch.NormalizeCode(row["Branch"]);

        if (code.Length == 0)
        {
            return (null, null);
        }

        if (!branches.TryGetValue(code, out var cached))
        {
            cached = await tenantUow.Repository<Branch>().Query()
                .Where(b => b.Code == code)
                .Select(b => (Guid?)b.Id)
                .FirstOrDefaultAsync(ct);

            branches[code] = cached;
        }

        return cached is null
            ? (null, MasterDataImportIssue.Field(row.Line, "Branch", $"There is no branch with code '{code}'."))
            : (cached, null);
    }
}
