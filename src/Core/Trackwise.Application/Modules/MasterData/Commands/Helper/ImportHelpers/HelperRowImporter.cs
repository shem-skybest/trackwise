using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a helper out of one file row.
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CreateHelperValidator"/>, so a file and the steward's form agree about what a legal
/// helper is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class HelperRowImporter(ITenantUnitOfWork tenantUow) : IMasterDataRowImporter
{
    /// <summary>
    /// Branch codes resolved so far. A file scoped to one office would otherwise re-query the same
    /// branch on every one of its rows.
    /// </summary>
    private readonly Dictionary<string, Guid?> branches = new(StringComparer.Ordinal);

    public string EntityType => nameof(Helper);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Code", false, "Leave blank to have one allocated.", "HLP-0001"),
        new("Name", true, "Full name as it should appear everywhere.", "Juan Dela Cruz"),
        new("PhoneNumber", false, "Contact number, if known.", "+63 917 123 4567"),
        new("Street", false, "Street address.", "123 Rizal St"),
        new("Subdivision", false, "Subdivision, if any.", ""),
        new("Barangay", false, "Barangay, if any.", "Barangay 649"),
        new("City", false, "City or municipality.", "Manila"),
        new("Province", false, "Province, state or region.", "Metro Manila"),
        new("Region", false, "Administrative region.", "NCR"),
        new("Zipcode", false, "Postal code.", "1018"),
        new("Country", false, "Two-letter ISO country code.", "PH"),
        new("Branch", false, "Code of the branch that maintains it. Blank means every branch.", "MNL"),
        new("Notes", false, "Anything a steward should know about this helper.", "")
    ];

    public async Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        var (branchId, branchIssue) = await ResolveBranchAsync(row, ct);

        if (branchIssue is not null)
        {
            return MasterDataRowBinding.Rejected(branchIssue);
        }

        return MasterDataRowBinding.For(new CreateHelperCommand
        {
            Code = row["Code"],
            Name = row["Name"] ?? "",
            PhoneNumber = row["PhoneNumber"],
            Address = ReadAddress(row),
            BranchId = branchId,
            Notes = row["Notes"],
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        });
    }

    /// <summary>
    /// The address columns, or null when the file carries none of them.
    /// <para>
    /// A helper's address is optional - not every one has a home address on file - so an empty block
    /// stays empty rather than becoming an address with every field blank. Once <i>any</i> of them
    /// is filled the block is built whole, blanks and all, and <c>AddressValidator</c> names the
    /// fields that are missing.
    /// </para>
    /// </summary>
    private static Address? ReadAddress(MasterDataImportRow row)
    {
        string?[] parts =
        [
            row["Street"], row["Subdivision"], row["Barangay"], row["City"],
            row["Province"], row["Region"], row["Zipcode"], row["Country"]
        ];

        if (parts.All(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        return new Address
        {
            StreetAddress = row["Street"] ?? "",
            Subdivision = row["Subdivision"],
            Barangay = row["Barangay"],
            CityOrMunicipality = row["City"] ?? "",
            Province = row["Province"] ?? "",
            Region = row["Region"] ?? "",
            Zipcode = row["Zipcode"] ?? "",
            Country = row["Country"]?.ToUpperInvariant() ?? ""
        };
    }

    /// <summary>
    /// Turns a branch code into its id. A file names the office the way a person does; an unknown
    /// code is rejected rather than quietly imported as a global helper, because "applies
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
