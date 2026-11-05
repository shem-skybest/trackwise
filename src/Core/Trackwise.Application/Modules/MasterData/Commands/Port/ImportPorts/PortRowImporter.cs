using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a port out of one file row. Ports are the first entity with a legacy table behind them -
/// <c>TBL_PORT</c> - so this is also the shape the remaining reference lists copy.
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CreatePortValidator"/>, so a file and the steward's form agree about what a legal
/// port is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class PortRowImporter(ITenantUnitOfWork tenantUow) : IMasterDataRowImporter
{
    /// <summary>
    /// Branch codes resolved so far. A file scoped to one office would otherwise re-query the same
    /// branch on every one of its rows.
    /// </summary>
    private readonly Dictionary<string, Guid?> branches = new(StringComparer.Ordinal);

    public string EntityType => nameof(Terminal);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Code", false, "UN/LOCODE. Leave blank to have one allocated.", "PHMNL"),
        new("Name", true, "Full port name as it should appear everywhere.", "Manila International Container Terminal"),
        new("Country", true, "Two-letter ISO country code.", "PH"),
        new("Type", true, $"One of: {string.Join(", ", Enum.GetNames<TerminalType>())}.", nameof(TerminalType.SeaPort)),
        new("Branch", false, "Code of the branch that maintains it. Blank means every branch.", "MNL"),
        new("Street", true, "Street address.", "South Access Road"),
        new("Subdivision", false, "Subdivision, if any.", ""),
        new("Barangay", false, "Barangay, if any.", "Barangay 649"),
        new("City", true, "City or municipality.", "Manila"),
        new("Province", true, "Province, state or region.", "Metro Manila"),
        new("Region", true, "Administrative region.", "NCR"),
        new("Zipcode", true, "Postal code. Four digits in the Philippines.", "1018"),
        new("Notes", false, "Anything a steward should know about this port.", "")
    ];

    public async Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        if (!Enum.TryParse<TerminalType>(row["Type"], ignoreCase: true, out var type))
        {
            return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                row.Line,
                "Type",
                $"'{row["Type"]}' is not a port type. Use one of: {string.Join(", ", Enum.GetNames<TerminalType>())}."));
        }

        var (branchId, branchIssue) = await ResolveBranchAsync(row, ct);

        if (branchIssue is not null)
        {
            return MasterDataRowBinding.Rejected(branchIssue);
        }

        var country = row["Country"] ?? "";

        return MasterDataRowBinding.For(new CreatePortCommand
        {
            Code = row["Code"],
            Name = row["Name"] ?? "",
            CountryCode = country.ToUpperInvariant(),
            Type = type,
            BranchId = branchId,
            Notes = row["Notes"],
            Address = new Address
            {
                StreetAddress = row["Street"] ?? "",
                Subdivision = row["Subdivision"],
                Barangay = row["Barangay"],
                CityOrMunicipality = row["City"] ?? "",
                Province = row["Province"] ?? "",
                Region = row["Region"] ?? "",
                Zipcode = row["Zipcode"] ?? "",

                // The port's ISO country doubles as the address country rather than asking for it
                // twice. A port in a different country from its own address is not a thing.
                Country = country.ToUpperInvariant()
            },
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        });
    }

    /// <summary>
    /// Turns a branch code into its id. A file names the office the way a person does; an unknown
    /// code is rejected rather than quietly imported as a global port, because "applies everywhere"
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
