using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

public enum TruckType
{
    Flatbed,
    FreightTruck,
    Reefer,
    Tanker,
    BoxTruck,
    DumpTruck,
    TowTruck,
    CarHauler,
    ContainerTruck,
    Tautliner,

    [Description("Low Loader")]
    LowLoader,

    [Description("Car Transporter")]
    CarTransporter,

    [Description("Swap Body")]
    SwapBody,

    Curtainsider,

    /// <summary>A rigid truck whose box sides lift open like wings for side loading - common in the Philippines.</summary>
    [Description("Wing Van")]
    WingVan
}
