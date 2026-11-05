using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding;

public enum Incoterm
{
    EXW = 1, // Ex Works
    FCA = 2, // Free Carrier
    FOB = 3, // Free On Board
    CIF = 4, // Cost, Insurance & Freight
    CPT = 5, // Carriage Paid To
    DAP = 6, // Delivered At Place
    DDP = 7  // Delivered Duty Paid
}
