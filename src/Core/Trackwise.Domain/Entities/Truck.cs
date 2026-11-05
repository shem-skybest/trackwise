using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// A truck, as a canonical master-data record. Extended in place rather than shadowed by a parallel
/// entity, the same way <see cref="Terminal"/> absorbed the port promotion - a second copy would
/// recreate the duplication the module exists to remove.
/// </summary>
public partial class Truck : AuditableEntity, ITenantEntity, IMasterDataRecord, IBranchScoped
{
    public MasterDataGroup Group => MasterDataGroup.FleetAndPersonnel;

    private string number = "";

    /// <summary>
    /// The truck's unit number (what dispatch and drivers call it) - this <i>is</i> the master
    /// <see cref="IMasterDataRecord.Code" />, exposed under its existing name rather than renamed:
    /// ~59 Application-layer call sites already read/write <c>Number</c>, and a mechanical rename
    /// bought nothing a forwarding interface implementation doesn't already give the module.
    /// <see cref="IMasterDataRecord.Code" /> is implemented explicitly below, forwarding here, so
    /// the generic master-data machinery (duplicate probe, change log, merge) sees the same value
    /// every existing caller does.
    /// <para>
    /// Canonicalised on write by <see cref="NormalizeCode" />, same as every other master code.
    /// </para>
    /// </summary>
    public required string Number
    {
        get => number;
        set => number = NormalizeCode(value);
    }

    /// <summary>Forwards to <see cref="Number" /> - see its doc comment for why.</summary>
    string IMasterDataRecord.Code
    {
        get => Number;
        set => Number = value;
    }

    public static string NormalizeCode(string? value) => value?.Trim().ToUpperInvariant() ?? "";

    /// <summary>
    /// Display name. Trucks have no descriptive name in the legacy data, so callers that create one
    /// without supplying it fall back to make/model/year, and to the code itself if even those are
    /// blank - see <c>CreateTruckHandler</c>. Backs the fuzzy duplicate rule the same way a port's
    /// name does.
    /// </summary>
    public required string Name { get; set; }

    public required TruckType Type { get; set; }

    /// <summary>
    ///     Maximum number of vehicles that can be carried by the car hauler truck.
    /// </summary>
    public int VehicleCapacity { get; set; }

    /// <summary>
    /// Payload-weight class (e.g. 2 Tonner / 4 Tonner / 10 Tonner) - the dimension a cold-chain
    /// distance-band rate card is keyed on. Null for body types with no tonnage concept.
    /// </summary>
    public TruckTonnageClass? TonnageClass { get; set; }

    /// <summary>
    /// Gross vehicle weight in kg, as printed on the CR. Decides which Philippine DL code a driver
    /// needs (B2 up to 3,500 kg, C above), which the payload <see cref="TonnageClass" /> can't: a
    /// "2 tonner" can land on either side depending on its body and reefer unit. Null until entered.
    /// </summary>
    public int? GrossVehicleWeightKg { get; set; }

    /// <summary>
    /// Operational state (available, en route, in maintenance, ...) - independent of
    /// <see cref="IsActive" />, which is the master-data "still part of the fleet" flag. A truck can
    /// be <see cref="TruckStatus.OutOfService" /> and still active, or retired (inactive) while its
    /// last known status stays whatever it was when retired.
    /// </summary>
    public TruckStatus Status { get; set; } = TruckStatus.Available;

    /// <summary>
    ///     Manufacturer name (e.g., Freightliner, Peterbilt, Kenworth, Volvo)
    /// </summary>
    public string? Make { get; set; }

    /// <summary>
    ///     Model name (e.g., Cascadia, 579, T680, VNL)
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    ///     Year of manufacture
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Vehicle Identification Number - globally unique and never reassigned, so where a truck has
    /// one on file it is the module's usual "second exact rule" alongside <see cref="Code" />, the
    /// same role <c>Vessel.ImoNumber</c> / <c>Trailer.Vin</c> play elsewhere.
    /// <para>
    /// Nullable because not every truck has one recorded. Stored upper-trimmed through
    /// <see cref="NormalizeVin" /> so "1gr" and "1GR" are the same value to the duplicate probe.
    /// </para>
    /// </summary>
    public string? Vin
    {
        get => vin;
        set => vin = NormalizeVin(value);
    }

    private string? vin;

    public static string? NormalizeVin(string? value)
    {
        var trimmed = value?.Trim().ToUpperInvariant();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    ///     Vehicle license plate number
    /// </summary>
    public string? LicensePlate { get; set; }

    /// <summary>
    ///     State where the license plate is registered (e.g., "TX", "CA")
    /// </summary>
    public string? LicensePlateState { get; set; }

    /// <summary>
    ///     ADR (Accord Dangereux Routier) equipment configuration. Tracks whether this
    ///     truck is certified to carry hazardous goods and which classes it is approved for.
    /// </summary>
    public AdrEquipment AdrEquipment { get; set; } = AdrEquipment.None;

    /// <summary>
    ///     US-side flag: truck is placarded for Hazmat transport (per FMCSA / DOT).
    /// </summary>
    public bool IsHazmatPlacarded { get; set; }

    /// <summary>
    ///     Truck's last known location address
    /// </summary>
    public Address? CurrentAddress { get; set; }

    /// <summary>
    ///     Truck's last known location longitude
    /// </summary>
    public GeoPoint? CurrentLocation { get; set; }

    /// <summary>
    ///     The main assigned driver for the truck
    /// </summary>
    public virtual Employee? MainDriver { get; set; }

    public Guid? MainDriverId { get; set; }

    /// <summary>
    ///     The secondary (backup/co) driver for the truck
    /// </summary>
    public virtual Employee? SecondaryDriver { get; set; }

    public Guid? SecondaryDriverId { get; set; }

    //public virtual List<Employee> Drivers { get; set; } = [];
    public virtual List<Load> Loads { get; } = [];
    public virtual List<Trip> Trips { get; } = [];
    public virtual List<TruckDocument> Documents { get; set; } = [];

    /// <summary>
    /// Owning branch, or null when every branch uses this truck. Scoping governs stewardship, not
    /// visibility - a truck one office maintains is still readable everywhere.
    /// </summary>
    public Guid? BranchId { get; set; }

    public virtual Branch? Branch { get; set; }

    /// <summary>
    /// Whether the truck is still part of the fleet. Distinct from <see cref="Status" /> (see above)
    /// and from deletion: a retired truck keeps every load, trip and expense it was ever part of.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public Guid? MergedIntoId { get; set; }

    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public static Truck Create(string truckNumber, TruckType type, Employee mainDriver,
        Employee? secondaryDriver = null)
    {
        return new Truck
        {
            Number = truckNumber,
            Name = truckNumber,
            Type = type,
            MainDriver = mainDriver,
            SecondaryDriver = secondaryDriver
        };
    }

    public IEnumerable<string> GetDriversNames()
    {
        if (MainDriver is { } mainDriver)
        {
            yield return mainDriver.GetFullName();
        }

        if (SecondaryDriver is { } secondaryDriver)
        {
            yield return secondaryDriver.GetFullName();
        }
    }

    /// <summary>
    ///     The total percentage of income that drivers can receive from total gross income. Value must be in the range [0, 1]
    /// </summary>
    public float GetDriversShareRatio()
    {
        float ratio = 0;

        if (MainDriver?.SalaryType == SalaryType.ShareOfGross)
        {
            ratio += (float)MainDriver.Salary.Amount;
        }

        if (SecondaryDriver?.SalaryType == SalaryType.ShareOfGross)
        {
            ratio += (float)SecondaryDriver.Salary.Amount;
        }

        return ratio;
    }
}
