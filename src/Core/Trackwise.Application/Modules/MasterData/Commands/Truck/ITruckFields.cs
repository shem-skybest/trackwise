using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>The truck fields create and update share, so <see cref="TruckValidatorBase{T}" /> checks them once.</summary>
public interface ITruckFields
{
    TruckType Type { get; }
    int? VehicleCapacity { get; }
    int? GrossVehicleWeightKg { get; }
    string? Vin { get; }
    string? LicensePlate { get; }
    string? LicensePlateState { get; }
    string? AssetCode { get; }
    string? ConductionSticker { get; }
    string? EngineNumber { get; }
    string? Supplier { get; }
    TruckOwnership? Ownership { get; }
}
