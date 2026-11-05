using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a commodity out of one file row. Like vessels, the file a steward loads here is the
/// distinct list of what is already sitting in <c>Workfile.Commodity</c> as free text.
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CreateCommodityValidator"/>, so a file and the steward's form agree about what a
/// legal commodity is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class CommodityRowImporter(ITenantUnitOfWork tenantUow) : IMasterDataRowImporter
{
    /// <summary>
    /// Branch codes resolved so far. A file scoped to one office would otherwise re-query the same
    /// branch on every one of its rows.
    /// </summary>
    private readonly Dictionary<string, Guid?> branches = new(StringComparer.Ordinal);

    public string EntityType => nameof(Commodity);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Code", false, "Leave blank to have one allocated.", "CMD-0001"),
        new("Name", true, "Commodity name as it should appear everywhere.", "Frozen Fish"),
        new("HsCode", false, "Harmonized System code, six to ten digits.", "0303.89"),
        new("Description", false, "Fuller description, if the name alone is ambiguous.", ""),
        new("Hazardous", false, "Yes or no. Blank means no.", "no"),
        new("UnNumber", false, "Four-digit UN number. Only for a hazardous commodity.", ""),
        new("TemperatureControlled", false, "Yes or no. Blank means no.", "yes"),
        new("Branch", false, "Code of the branch that maintains it. Blank means every branch.", "MNL"),
        new("Notes", false, "Anything a steward should know about this commodity.", "")
    ];

    public async Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        var (hazardous, hazardIssue) = ReadFlag(row, "Hazardous");

        if (hazardIssue is not null)
        {
            return MasterDataRowBinding.Rejected(hazardIssue);
        }

        var (reefer, reeferIssue) = ReadFlag(row, "TemperatureControlled");

        if (reeferIssue is not null)
        {
            return MasterDataRowBinding.Rejected(reeferIssue);
        }

        var (branchId, branchIssue) = await ResolveBranchAsync(row, ct);

        if (branchIssue is not null)
        {
            return MasterDataRowBinding.Rejected(branchIssue);
        }

        return MasterDataRowBinding.For(new CreateCommodityCommand
        {
            Code = row["Code"],
            Name = row["Name"] ?? "",
            HsCode = row["HsCode"],
            Description = row["Description"],
            IsHazardous = hazardous,
            UnNumber = row["UnNumber"],
            RequiresTemperatureControl = reefer,
            BranchId = branchId,
            Notes = row["Notes"],
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        });
    }

    /// <summary>
    /// Reads a yes/no column. Spreadsheets spell a boolean six ways, so all six are accepted and
    /// anything else is rejected by name - silently treating "Y" as false would import a hazardous
    /// commodity as ordinary freight.
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

    /// <summary>
    /// Turns a branch code into its id. A file names the office the way a person does; an unknown
    /// code is rejected rather than quietly imported as a global commodity, because "applies
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
