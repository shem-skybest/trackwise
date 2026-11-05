using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Which reporting bucket a workfile belongs to. Must be chosen before the file is first saved.
/// </summary>
public enum ReportCategory
{
    [Description("PSB")]
    Psb = 0,

    [Description("Not PSB")]
    NotPsb = 1
}
