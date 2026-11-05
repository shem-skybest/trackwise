using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Creates a truck. The unit number is exposed as <c>Number</c>, not <c>Code</c> - see
/// <c>Truck.Number</c>'s doc comment for why the module's usual field name was not adopted here.
/// </summary>
public class CreateTruckCommand : ICommand<Result<TruckDto>>, IMasterDataWriteCommand, ITruckFields
{
    /// <summary>
    /// The truck's unit number. Leave blank to have one allocated from the <c>Truck</c> number
    /// series.
    /// </summary>
    public string? Number { get; set; }

    /// <summary>
    /// Display name. Leave blank to fall back to make/model/year, and to the unit number itself if
    /// even those are blank.
    /// </summary>
    public string? Name { get; set; }

    public TruckType Type { get; set; }
    public int? VehicleCapacity { get; set; }
    public TruckTonnageClass? TonnageClass { get; set; }
    /// <summary>Gross vehicle weight in kg, from the CR. Decides the Philippine DL code a driver needs.</summary>
    public int? GrossVehicleWeightKg { get; set; }
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
    public TruckOwnership? Ownership { get; set; }
    public AdrEquipmentDto? AdrEquipment { get; set; }
    public bool IsHazmatPlacarded { get; set; }

    /// <summary>
    /// The main assigned driver, if known at creation. Unlike the legacy CRUD slice, a truck is not
    /// required to have one on create - a spare unit can be registered before a driver is assigned.
    /// </summary>
    public Guid? MainDriverId { get; set; }

    public Guid? SecondaryDriverId { get; set; }

    /// <summary>
    /// Owning branch, or null when every branch uses this truck. Scoping governs who maintains the
    /// record, not who can read it.
    /// </summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Set after the steward has seen the duplicate candidates and asserts this is a different
    /// truck. Requires <see cref="DistinctReason"/>, so the override is auditable.
    /// </summary>
    public bool ConfirmedDistinct { get; set; }

    public string? DistinctReason { get; set; }

    string IMasterDataWriteCommand.MasterDataEntityType => nameof(Domain.Entities.Truck);

    Guid? IMasterDataWriteCommand.MasterDataRecordId => null;

    IReadOnlyDictionary<string, string?> IMasterDataWriteCommand.DedupeFields => new Dictionary<string, string?>
    {
        ["Code"] = Number,
        ["Vin"] = Vin,
        ["Name"] = Name
    };
}
