using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums.Safety;

public enum DvirType
{
    [Description("Pre-Trip Inspection")]
    PreTrip,

    [Description("Post-Trip Inspection")]
    PostTrip
}
