namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Payload-weight class for a reefer/trucking unit (e.g. "2 Tonner", "4 Tonner"). Distinct from
/// <see cref="TruckType"/> (the body style) and from <c>VehicleCapacity</c> (car-hauler-specific
/// vehicle count) - this is the dimension a cold-chain distance-band rate card is keyed on.
/// Nullable on <c>Truck</c>: most body types have no tonnage concept at all.
/// </summary>
public enum TruckTonnageClass
{
    TwoTonner,
    FourTonner,
    TenTonner,

    /// <summary>Quoted by some clients (Pacific Food) between the 2T and 4T units.</summary>
    ThreeTonner
}
