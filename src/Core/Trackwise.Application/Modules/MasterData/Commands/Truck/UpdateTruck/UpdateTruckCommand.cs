using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Modules.MasterData.Common;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Edits a truck. Carries no <c>Number</c>: it is the handle other systems join on, so a rename
/// would strand every legacy key pointing at this record.
/// </summary>
public class UpdateTruckCommand : ICommand<Result>, IHaveId, IMasterDataDeactivateCommand, ITruckFields
{
    public Guid Id { get; set; }

    public string Name { get; set; } = default!;
    public TruckType Type { get; set; }
    public TruckStatus Status { get; set; }
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
    public Guid? MainDriverId { get; set; }
    public Guid? SecondaryDriverId { get; set; }
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Deactivating keeps the record and every load, trip and expense that references it; it only
    /// stops the truck being offered for new work. Use delete to remove it from ordinary views.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Set after the steward has seen what still points at this truck and is deactivating it anyway.
    /// Requires <see cref="DependencyReason"/>. Ignored when the truck stays active.
    /// </summary>
    public bool ConfirmedDespiteDependents { get; set; }

    public string? DependencyReason { get; set; }

    string IMasterDataDeactivateCommand.MasterDataEntityType => nameof(Domain.Entities.Truck);

    Guid IMasterDataDeactivateCommand.MasterDataRecordId => Id;

    MasterDataDeactivation IMasterDataDeactivateCommand.Deactivation =>
        IsActive ? MasterDataDeactivation.None : MasterDataDeactivation.Deactivate;
}
