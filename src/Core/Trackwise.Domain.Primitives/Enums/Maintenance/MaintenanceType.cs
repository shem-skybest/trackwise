using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums.Maintenance;

public enum MaintenanceType
{
    OilChange,
    TireRotation,
    TireReplacement,
    BrakeInspection,
    BrakeReplacement,
    AirFilterReplacement,
    FuelFilterReplacement,
    TransmissionService,
    CoolantFlush,
    BeltInspection,

    [Description("Battery Check/Replacement")]
    Battery,

    [Description("Annual DOT Inspection")]
    AnnualDotInspection,

    PreventiveMaintenance,
    EngineService,
    SuspensionService,
    ElectricalRepair,
    BodyWork,

    [Description("HVAC Service")]
    HvacService,

    ExhaustSystem,
    SteeringRepair,
    Other,

    /// <summary>A breakdown or wear repair - which part of the truck is in <see cref="VehicleSystem" />.</summary>
    Repair,

    /// <summary>The reefer unit's routine preventive maintenance, done weekly by Cold Chain.</summary>
    [Description("Reefer PMS")]
    ReeferPms
}
