using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding;

public enum BreakpointMode
{
    /// <summary>
    /// The tier matching the total quantity prices ALL units (typical for air freight:
    /// 1,250 kg at the 500+ rate = 1,250 × rate).
    /// </summary>
    AllUnits = 0,

    /// <summary>
    /// Marginal brackets: each tier prices only the units that fall inside it
    /// (typical for tiered storage / handling).
    /// </summary>
    Incremental = 1
}

