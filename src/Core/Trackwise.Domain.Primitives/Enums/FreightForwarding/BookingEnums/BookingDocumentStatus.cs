using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

public enum BookingDocumentStatus
{
    Required = 0,
    Submitted = 1,
    Verified = 2,
    Rejected = 3,
    Waived = 4
}
