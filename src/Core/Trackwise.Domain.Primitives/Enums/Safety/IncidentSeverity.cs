using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums.Safety;

public enum IncidentSeverity
{
    [Description("Minor - no injuries, small loss")]
    Minor,

    [Description("Moderate - minor injuries or a real loss")]
    Moderate,

    [Description("Severe - serious injuries or a major loss")]
    Severe,

    [Description("Fatal - someone died")]
    Fatal
}
