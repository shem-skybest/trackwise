using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums;

public enum DutyStatus
{
    OffDuty = 0,
    SleeperBerth = 1,
    Driving = 2,

    [Description("On Duty (Not Driving)")]
    OnDutyNotDriving = 3,

    YardMove = 4,
    PersonalConveyance = 5
}
