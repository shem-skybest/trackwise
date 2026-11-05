using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding;

public enum OperationalServiceType
{
    Standard = 0,
    LineHaul = 1,

    // Cold Chain specific
    PreCoolingAndPTI = 10,
    GensetFuel = 11,
    ContinuousTelematics = 12,
    ColdStoragePlugin = 13,

    // General Freight / Ocean / Air specific
    TerminalHandlingOrigin = 20,
    TerminalHandlingDestination = 21,
    CustomsClearanceExport = 22,
    CustomsClearanceImport = 23,
    BunkerAdjustmentFactor = 24,
    SecurityFilingISF = 25,
    DemurrageDetention = 26,
    LiftgatePickupDelivery = 27,
    HazmatSurcharge = 28
}
