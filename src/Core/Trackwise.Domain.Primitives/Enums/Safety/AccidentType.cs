using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums.Safety;

public enum AccidentType
{
    Collision,
    Rollover,
    Jackknife,
    RunOffRoad,
    RearEnd,
    Sideswipe,

    [Description("Head-On")]
    HeadOn,

    HitAndRun,
    PedestrianInvolved,
    PropertyDamageOnly,
    CargoSpill,
    Other
}
