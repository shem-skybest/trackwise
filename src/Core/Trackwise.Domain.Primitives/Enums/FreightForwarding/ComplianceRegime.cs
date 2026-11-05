using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding;

public enum ComplianceRegime
{
    Standard = 1,
    GDP = 2,    // Good Distribution Practice (Pharma)
    FSMA = 3,   // Food Safety Modernization Act
    HACCP = 4
}
