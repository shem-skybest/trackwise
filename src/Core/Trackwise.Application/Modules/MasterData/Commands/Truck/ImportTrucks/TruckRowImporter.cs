using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a truck out of one file row.
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CreateTruckValidator"/>, so a file and the steward's form agree about what a legal
/// truck is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class TruckRowImporter(ITenantUnitOfWork tenantUow) : IMasterDataRowImporter
{
    /// <summary>
    /// Branch codes resolved so far. A file scoped to one office would otherwise re-query the same
    /// branch on every one of its rows.
    /// </summary>
    private readonly Dictionary<string, Guid?> branches = new(StringComparer.Ordinal);

    public string EntityType => nameof(Truck);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Number", false, "Unit number. Leave blank to have one allocated.", "T-101"),
        new("Name", false, "Descriptive label. Falls back to make/model/year, then the number.", "2022 Freightliner Cascadia"),
        new("Type", true, $"One of: {string.Join(", ", Enum.GetNames<TruckType>())}.", nameof(TruckType.FreightTruck)),
        new("Vin", false, "17-character VIN, if known.", "1FUJA6CV88LX12345"),
        new("LicensePlate", false, "Plate number, if any.", "TRK-8821"),
        new("LicensePlateState", false, "State or province the plate is registered in.", "TX"),
        new("Make", false, "Manufacturer.", "Freightliner"),
        new("Model", false, "Model.", "Cascadia"),
        new("Year", false, "Year of manufacture.", "2022"),
        new("VehicleCapacity", false, "Maximum number of vehicles carried (car haulers only).", "0"),
        new("Branch", false, "Code of the branch that maintains it. Blank means every branch.", "MNL")
    ];

    public async Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        if (!Enum.TryParse<TruckType>(row["Type"], ignoreCase: true, out var type))
        {
            return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                row.Line,
                "Type",
                $"'{row["Type"]}' is not a truck type. Use one of: {string.Join(", ", Enum.GetNames<TruckType>())}."));
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

        var vehicleCapacity = (int?)null;

        if (!string.IsNullOrWhiteSpace(row["VehicleCapacity"]))
        {
            if (!int.TryParse(row["VehicleCapacity"], out var parsedCapacity))
            {
                return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                    row.Line, "VehicleCapacity", $"'{row["VehicleCapacity"]}' is not a whole number."));
            }

            vehicleCapacity = parsedCapacity;
        }

        var (branchId, branchIssue) = await ResolveBranchAsync(row, ct);

        if (branchIssue is not null)
        {
            return MasterDataRowBinding.Rejected(branchIssue);
        }

        return MasterDataRowBinding.For(new CreateTruckCommand
        {
            Number = row["Number"],
            Name = row["Name"],
            Type = type,
            Vin = row["Vin"],
            LicensePlate = row["LicensePlate"],
            LicensePlateState = row["LicensePlateState"],
            Make = row["Make"],
            Model = row["Model"],
            Year = year,
            VehicleCapacity = vehicleCapacity,
            BranchId = branchId,
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        });
    }

    /// <summary>
    /// Turns a branch code into its id. A file names the office the way a person does; an unknown
    /// code is rejected rather than quietly imported as a global truck, because "applies everywhere"
    /// is a decision, not a fallback for a typo.
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
