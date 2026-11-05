using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public record TruckDto
{
    public Guid? Id { get; set; }
    public string? Number { get; set; }

    /// <summary>Display name. Master-data field - see <c>Truck.Name</c>.</summary>
    public string? Name { get; set; }

    public int VehicleCapacity { get; set; }
    public TruckTonnageClass? TonnageClass { get; set; }
    public int? GrossVehicleWeightKg { get; set; }
    public TruckType Type { get; set; }
    public TruckStatus Status { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? Vin { get; set; }
    public string? LicensePlate { get; set; }
    public string? LicensePlateState { get; set; }
    public string? AssetCode { get; set; }
    public string? ConductionSticker { get; set; }
    public string? EngineNumber { get; set; }
    public DateOnly? AcquiredOn { get; set; }
    public string? Supplier { get; set; }

    /// <summary>Null when nobody has confirmed whether the unit is owned, leased or subcontracted.</summary>
    public TruckOwnership? Ownership { get; set; }

    public AdrEquipmentDto AdrEquipment { get; set; } = new();
    public bool IsHazmatPlacarded { get; set; }
    public Address? CurrentAddress { get; set; }
    public GeoPoint? CurrentLocation { get; set; }
    public EmployeeDto? MainDriver { get; set; }
    public EmployeeDto? SecondaryDriver { get; set; }
    public IEnumerable<LoadDto> Loads { get; set; } = [];

    /// <summary>Owning branch, or null when the truck applies to every branch.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Resolved by the handler in one batched lookup, never off the navigation property.</summary>
    public string? BranchName { get; set; }

    /// <summary>Whether the truck is still part of the fleet. Master-data field, distinct from <see cref="Status" />.</summary>
    public bool IsActive { get; set; }

    /// <summary>Set when a steward merged this truck into another. Non-null means the record is a tombstone.</summary>
    public Guid? MergedIntoId { get; set; }

    /// <summary>Non-null for a soft-deleted record. Only ever populated on a deleted-records view.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Set when the caller asked for an id that had been merged away and this record is the
    /// survivor it forwarded to.
    /// </summary>
    public Guid? ForwardedFromId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
