using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a vessel out of one file row.
/// <para>
/// The first importer with no legacy table to import <i>from</i>. What a steward loads here is a
/// distinct list of the names already sitting in <c>Workfile.VesselName</c>, which is why the
/// duplicate probe matters more on this file than on any other: the source is the sprawl.
/// </para>
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CreateVesselValidator"/>, so a file and the steward's form agree about what a legal
/// vessel is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class VesselRowImporter(ITenantUnitOfWork tenantUow) : IMasterDataRowImporter
{
    /// <summary>
    /// Branch codes resolved so far. A file scoped to one office would otherwise re-query the same
    /// branch on every one of its rows.
    /// </summary>
    private readonly Dictionary<string, Guid?> branches = new(StringComparer.Ordinal);

    public string EntityType => nameof(Vessel);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Code", false, "Leave blank to have one allocated.", "VSL-0001"),
        new("Name", true, "Vessel name as it should appear everywhere.", "Ever Given"),
        new("IMO", false, "Seven-digit IMO number. Blank for craft that have none.", "9811000"),
        new("Type", true, $"One of: {string.Join(", ", Enum.GetNames<VesselType>())}.", nameof(VesselType.ContainerShip)),
        new("CallSign", false, "Radio call sign, if known.", "H3RC"),
        new("Flag", false, "Two-letter ISO country of registry.", "PA"),
        new("TeuCapacity", false, "Nominal TEU capacity. Blank where it does not apply.", "20124"),
        new("Branch", false, "Code of the branch that maintains it. Blank means every branch.", "MNL"),
        new("Notes", false, "Anything a steward should know about this vessel.", "")
    ];

    public async Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        if (!Enum.TryParse<VesselType>(row["Type"], ignoreCase: true, out var type))
        {
            return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                row.Line,
                "Type",
                $"'{row["Type"]}' is not a vessel type. Use one of: {string.Join(", ", Enum.GetNames<VesselType>())}."));
        }

        // Parsed rather than left to the validator, because the column is an int: a non-numeric
        // value has no valid command to bind to at all, and "not a number" is a clearer answer than
        // a silently dropped capacity.
        int? teu = null;

        if (!string.IsNullOrWhiteSpace(row["TeuCapacity"]))
        {
            if (!int.TryParse(row["TeuCapacity"], out var parsed))
            {
                return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                    row.Line, "TeuCapacity", $"'{row["TeuCapacity"]}' is not a whole number."));
            }

            teu = parsed;
        }

        var (branchId, branchIssue) = await ResolveBranchAsync(row, ct);

        if (branchIssue is not null)
        {
            return MasterDataRowBinding.Rejected(branchIssue);
        }

        return MasterDataRowBinding.For(new CreateVesselCommand
        {
            Code = row["Code"],
            Name = row["Name"] ?? "",
            ImoNumber = row["IMO"],
            CallSign = row["CallSign"],
            FlagCountryCode = row["Flag"]?.Trim().ToUpperInvariant(),
            Type = type,
            TeuCapacity = teu,
            BranchId = branchId,
            Notes = row["Notes"],
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        });
    }

    /// <summary>
    /// Turns a branch code into its id. A file names the office the way a person does; an unknown
    /// code is rejected rather than quietly imported as a global vessel, because "applies
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
