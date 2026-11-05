using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding;

public enum ShipmentType
{
    // Road / Rail
    FTL = 1, // Full Truckload
    LTL = 2, // Less Than Truckload

    // Ocean
    FCL = 3, // Full Container Load
    LCL = 4, // Less Container Load

    // Air
    AirStandard = 5,
    AirExpress = 6,
    AirCharter = 7
}
