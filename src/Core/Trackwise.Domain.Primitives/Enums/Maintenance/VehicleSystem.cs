namespace Trackwise.Domain.Primitives.Enums.Maintenance;

/// <summary>
/// The part of the vehicle a service or repair was on - what Motorpool's repair requisitions call
/// the "System". Kept apart from <see cref="MaintenanceType" /> so a reefer unit's repairs can be
/// told from the truck's own, whatever the job was.
/// </summary>
public enum VehicleSystem
{
    Engine,

    /// <summary>Clutch, gearbox and drive line.</summary>
    Drivetrain,

    Brakes,
    Suspension,
    Steering,
    Electrical,

    /// <summary>The engine's cooling: radiator, hoses, fan.</summary>
    Cooling,

    /// <summary>The refrigeration unit on the body - compressor, condenser, evaporator, controls.</summary>
    Reefer,

    Tires,
    Body,
    Other
}
