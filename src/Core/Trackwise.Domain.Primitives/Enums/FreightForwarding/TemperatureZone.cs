using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding;

public enum TemperatureZone
{
    Ambient = 1,
    ControlledRoomTemperature = 2, // 15°C to 25°C
    Chilled = 3,                   // 2°C to 8°C
    Frozen = 4,                    // -20°C to -10°C
    Cryogenic = 5                  // -80°C to -20°C (Dry Ice / LN2)
}
