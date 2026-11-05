using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a business partner out of one file row.
/// <para>
/// The first importer whose rows do <b>not</b> land live. Every row is created as a draft, exactly
/// as the form creates one, because an import that published four hundred partners without anybody
/// looking at them would be the largest hole maker-checker could have. The steward submits them
/// afterwards; the file is a faster way to type, not a way around the second person.
/// </para>
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CreatePartnerValidator"/>, so a file and the steward's form agree about what a legal
/// partner is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class PartnerRowImporter(ITenantUnitOfWork tenantUow) : IMasterDataRowImporter
{
    /// <summary>
    /// Branch codes resolved so far. A file scoped to one office would otherwise re-query the same
    /// branch on every one of its rows.
    /// </summary>
    private readonly Dictionary<string, Guid?> branches = new(StringComparer.Ordinal);

    public string EntityType => nameof(BusinessPartner);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Code", false, "Leave blank to have one allocated.", "BP-000001"),
        new("Name", true, "Registered or trading name.", "Maersk Filipinas Inc."),
        new(
            "Roles",
            true,
            $"One or more of: {string.Join(", ", Enum.GetNames<BusinessPartnerRole>())}. Separate with a space.",
            $"{nameof(BusinessPartnerRole.ShippingLine)} {nameof(BusinessPartnerRole.Agent)}"),
        new("TIN", false, "VAT ID / TIN. Punctuation is ignored.", "123-456-789-000"),
        new("ContactName", false, "Who to call there.", "Ana Reyes"),
        new("Email", false, "Contact email.", "ops@example.com"),
        new("Phone", false, "Contact number.", "+63 2 8888 0000"),
        new("Street", false, "Street address. Required once any address column is filled.", "2nd Ave"),
        new("Subdivision", false, "Subdivision, if any.", ""),
        new("Barangay", false, "Barangay, if any.", "Bonifacio Global City"),
        new("City", false, "City or municipality.", "Taguig"),
        new("Province", false, "Province, state or region.", "Metro Manila"),
        new("Region", false, "Administrative region.", "NCR"),
        new("Zipcode", false, "Postal code. Four digits in the Philippines.", "1634"),
        new("Country", false, "Country code.", "PH"),
        new("Branch", false, "Code of the branch that maintains it. Blank means every branch.", "MNL"),
        new("Notes", false, "Anything a steward should know about this partner.", "")
    ];

    public async Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        var (roles, roleIssue) = ParseRoles(row);

        if (roleIssue is not null)
        {
            return MasterDataRowBinding.Rejected(roleIssue);
        }

        var (branchId, branchIssue) = await ResolveBranchAsync(row, ct);

        if (branchIssue is not null)
        {
            return MasterDataRowBinding.Rejected(branchIssue);
        }

        return MasterDataRowBinding.For(new CreatePartnerCommand
        {
            Code = row["Code"],
            Name = row["Name"] ?? "",
            Roles = roles,
            TaxId = row["TIN"],
            ContactName = row["ContactName"],
            Email = row["Email"],
            Phone = row["Phone"],
            Address = ReadAddress(row),
            BranchId = branchId,
            Notes = row["Notes"],
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        });
    }

    /// <summary>
    /// A partner acts in several capacities at once, so the column is a list rather than a value.
    /// Space-separated because the delimiter sniffing already claims commas, semicolons, tabs and
    /// pipes - a comma here would split the row instead of the cell in half the files that arrive.
    /// </summary>
    private static (List<BusinessPartnerRole> Roles, MasterDataImportIssue? Issue) ParseRoles(
        MasterDataImportRow row)
    {
        var raw = row["Roles"];
        var roles = new List<BusinessPartnerRole>();

        if (string.IsNullOrWhiteSpace(raw))
        {
            // Left to the validator: "a partner must have at least one role" is a field rule the
            // form states too, and restating it here is how a file and a form start disagreeing.
            return (roles, null);
        }

        foreach (var word in raw.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Enum.TryParse<BusinessPartnerRole>(word, ignoreCase: true, out var role))
            {
                return (roles, MasterDataImportIssue.Field(
                    row.Line,
                    "Roles",
                    $"'{word}' is not a partner role. Use one or more of: "
                    + $"{string.Join(", ", Enum.GetNames<BusinessPartnerRole>())}."));
            }

            roles.Add(role);
        }

        return (roles.Distinct().ToList(), null);
    }

    /// <summary>
    /// The address columns, or null when the file carries none of them.
    /// <para>
    /// A partner's address is optional in a way a port's is not - a broker recorded by name and TIN
    /// alone is a legitimate record - so an empty block stays empty rather than becoming an address
    /// with every field blank, which reads later as a fact somebody established.
    /// </para>
    /// <para>
    /// Once <i>any</i> of them is filled the block is built whole, blanks and all, and
    /// <c>AddressValidator</c> names the fields that are missing. Rejecting a partial address here
    /// would restate the field rules the form already states, and the two would drift.
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
    /// code is rejected rather than quietly imported as a global partner, because "applies
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
