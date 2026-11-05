using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

public enum BookingChargeSource
{
    Quoted = 0,      // copied (locked) from the accepted quotation
    Additional = 1   // added after booking (demurrage, detention, extra handling...)
}
