using Trackwise.Application.Abstractions.Modules.MasterData;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Reads a location out of one file row. The legacy source is <c>TBL_DELIVERY_LOC_CONFIG</c> plus
/// the distinct origins and destinations sitting in <c>TBL_CUSTOMER_SERVICE</c> as free text.
/// <para>
/// It resolves and rejects; it never validates. Every field rule stays in
/// <see cref="CreateLocationValidator"/>, so a file and the steward's form agree about what a legal
/// location is without the two having to be kept in step by hand.
/// </para>
/// </summary>
internal sealed class LocationRowImporter(ITenantUnitOfWork tenantUow) : IMasterDataRowImporter
{
    private readonly Dictionary<string, Guid?> branches = new(StringComparer.Ordinal);

    /// <summary>
    /// Zone codes resolved so far. Unlike the branch cache this one genuinely fills up mid-run: a
    /// file that creates its zones ahead of its delivery points resolves each zone once and then
    /// reads it from here for every row beneath it.
    /// </summary>
    private readonly Dictionary<string, Guid?> zones = new(StringComparer.Ordinal);

    private readonly Dictionary<string, Guid?> customers = new(StringComparer.OrdinalIgnoreCase);

    public string EntityType => nameof(Location);

    public IReadOnlyList<MasterDataImportColumn> Columns { get; } =
    [
        new("Code", false, "Leave blank to have one allocated.", "LOC-0001"),
        new("Name", true, "Location name as it should appear everywhere.", "Cavite Export Processing Zone"),
        new("Type", true, $"One of: {string.Join(", ", Enum.GetNames<LocationType>())}.", nameof(LocationType.DeliveryPoint)),
        new("Zone", false, "Code of the zone this sits in. Blank for none.", "LOC-ZONE-S"),
        new("Street", false, "Street address. Required for everything except a zone.", "Gov. Drive"),
        new("Subdivision", false, "Subdivision, if any.", ""),
        new("Barangay", false, "Barangay, if any.", "Rosario"),
        new("City", false, "City or municipality.", "Rosario"),
        new("Province", false, "Province, state or region.", "Cavite"),
        new("Region", false, "Administrative region.", "CALABARZON"),
        new("Zipcode", false, "Postal code.", "4106"),
        new("Country", false, "Two-letter ISO country code. Defaults to PH.", "PH"),
        new("Latitude", false, "Decimal degrees. Give both coordinates or neither.", "14.4167"),
        new("Longitude", false, "Decimal degrees.", "120.8500"),
        new("Branch", false, "Code of the branch that maintains it. Blank means every branch.", "MNL"),
        new("Customer", false, "Name of the client whose outlet this is. Blank means any client can use it.", "Hacienda Verde Inc."),
        new("Cutoff", false, "Usual deliver-by time, 24-hour HH:mm.", "10:00"),
        new("ZoneRadius", false, "Arrival geofence radius in metres.", "150"),
        new("Notes", false, "Anything a steward should know about this location.", "")
    ];

    public async Task<MasterDataRowBinding> BindAsync(
        MasterDataImportRow row, MasterDataImportOverride overrides, CancellationToken ct = default)
    {
        if (!Enum.TryParse<LocationType>(row["Type"], ignoreCase: true, out var type))
        {
            return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                row.Line,
                "Type",
                $"'{row["Type"]}' is not a location type. Use one of: {string.Join(", ", Enum.GetNames<LocationType>())}."));
        }

        var (latitude, latitudeIssue) = ReadCoordinate(row, "Latitude");

        if (latitudeIssue is not null)
        {
            return MasterDataRowBinding.Rejected(latitudeIssue);
        }

        var (longitude, longitudeIssue) = ReadCoordinate(row, "Longitude");

        if (longitudeIssue is not null)
        {
            return MasterDataRowBinding.Rejected(longitudeIssue);
        }

        var (zoneId, zoneIssue) = await ResolveZoneAsync(row, ct);

        if (zoneIssue is not null)
        {
            return MasterDataRowBinding.Rejected(zoneIssue);
        }

        var (branchId, branchIssue) = await ResolveBranchAsync(row, ct);

        if (branchIssue is not null)
        {
            return MasterDataRowBinding.Rejected(branchIssue);
        }

        var (customerId, customerIssue) = await ResolveCustomerAsync(row, ct);

        if (customerIssue is not null)
        {
            return MasterDataRowBinding.Rejected(customerIssue);
        }

        var cutoffText = row["Cutoff"]?.Trim();
        TimeOnly? cutoff = null;

        if (!string.IsNullOrEmpty(cutoffText))
        {
            if (!TimeOnly.TryParseExact(cutoffText, ["H:mm", "HH:mm"], out var parsedCutoff))
            {
                return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                    row.Line, "Cutoff", $"'{cutoffText}' is not a 24-hour time such as 10:00."));
            }

            cutoff = parsedCutoff;
        }

        var radiusText = row["ZoneRadius"]?.Trim();
        int? radius = null;

        if (!string.IsNullOrEmpty(radiusText))
        {
            if (!int.TryParse(radiusText, out var parsedRadius))
            {
                return MasterDataRowBinding.Rejected(MasterDataImportIssue.Field(
                    row.Line, "ZoneRadius", $"'{radiusText}' is not a whole number of metres."));
            }

            radius = parsedRadius;
        }

        return MasterDataRowBinding.For(new CreateLocationCommand
        {
            Code = row["Code"],
            Name = row["Name"] ?? "",
            Type = type,

            // Null for a zone, which is what the validator requires - building an empty Address for
            // it would trip the "a zone has no address" rule on every zone row in the file.
            Address = type is LocationType.Zone ? null : ReadAddress(row),

            ParentLocationId = zoneId,
            Latitude = latitude,
            Longitude = longitude,
            BranchId = branchId,
            CustomerId = customerId,
            DefaultCutoffTime = cutoff,
            ZoneRadiusMeters = radius,
            Notes = row["Notes"],
            ConfirmedDistinct = overrides.ConfirmDistinct,
            DistinctReason = overrides.Reason
        });
    }

    private static Address ReadAddress(MasterDataImportRow row) => new()
    {
        StreetAddress = row["Street"] ?? "",
        Subdivision = row["Subdivision"],
        Barangay = row["Barangay"],
        CityOrMunicipality = row["City"] ?? "",
        Province = row["Province"] ?? "",
        Region = row["Region"] ?? "",
        Zipcode = row["Zipcode"] ?? "",

        // The legacy delivery-location table is entirely domestic and carries no country column at
        // all, so blank means PH rather than blank. A file from anywhere else says so explicitly.
        Country = string.IsNullOrWhiteSpace(row["Country"])
            ? "PH"
            : row["Country"]!.Trim().ToUpperInvariant()
    };

    /// <summary>
    /// Parses one coordinate. Rejected by name rather than dropped: a delivery point that silently
    /// lost its coordinates looks identical to one that never had any.
    /// </summary>
    private static (double? Value, MasterDataImportIssue? Issue) ReadCoordinate(
        MasterDataImportRow row, string column)
    {
        var raw = row[column]?.Trim();

        if (string.IsNullOrEmpty(raw))
        {
            return (null, null);
        }

        return double.TryParse(raw, out var parsed)
            ? (parsed, null)
            : (null, MasterDataImportIssue.Field(row.Line, column, $"'{raw}' is not a number."));
    }

    /// <summary>
    /// Turns a zone code into its id. Looked up among live rows only and re-checked for actually
    /// being a zone, because the file names it the way a person does - and a delivery point named
    /// as a parent is the mistake worth catching at the row rather than after the import.
    /// </summary>
    private async Task<(Guid? ZoneId, MasterDataImportIssue? Issue)> ResolveZoneAsync(
        MasterDataImportRow row, CancellationToken ct)
    {
        var code = Location.NormalizeCode(row["Zone"]);

        if (code.Length == 0)
        {
            return (null, null);
        }

        if (!zones.TryGetValue(code, out var cached))
        {
            cached = await tenantUow.Repository<Location>().Query()
                .Where(l => l.Code == code && l.Type == LocationType.Zone)
                .Select(l => (Guid?)l.Id)
                .FirstOrDefaultAsync(ct);

            zones[code] = cached;
        }

        return cached is null
            ? (null, MasterDataImportIssue.Field(row.Line, "Zone", $"There is no zone with code '{code}'."))
            : (cached, null);
    }

    /// <summary>
    /// Turns a customer name into its id. Customers have no code, so the name is matched exactly,
    /// ignoring case - a near miss is rejected rather than guessed, because an outlet filed under
    /// the wrong client is offered on the wrong client's loads.
    /// </summary>
    private async Task<(Guid? CustomerId, MasterDataImportIssue? Issue)> ResolveCustomerAsync(
        MasterDataImportRow row, CancellationToken ct)
    {
        var name = row["Customer"]?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            return (null, null);
        }

        if (!customers.TryGetValue(name, out var cached))
        {
            var lowered = name.ToLower();

            cached = await tenantUow.Repository<Customer>().Query()
                .Where(c => c.Name.ToLower() == lowered)
                .Select(c => (Guid?)c.Id)
                .FirstOrDefaultAsync(ct);

            customers[name] = cached;
        }

        return cached is null
            ? (null, MasterDataImportIssue.Field(row.Line, "Customer", $"There is no customer named '{name}'."))
            : (cached, null);
    }

    /// <summary>
    /// Turns a branch code into its id. An unknown code is rejected rather than quietly imported as
    /// a global location, because "applies everywhere" is a decision, not a fallback for a typo.
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
