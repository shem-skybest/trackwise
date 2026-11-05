namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// What a unit is doing on one day of the dispatcher's crew plan - the "Unit Status" of a row on
/// Monday's CCL OPS - DISPATCH board, with its free-text labels folded into one value each.
/// </summary>
public enum TruckDayStatus
{
    Available,

    /// <summary>Kept in reserve for the day, crewed or not - Monday's "BUFFER UNIT".</summary>
    Buffer,

    /// <summary>The unit is fine but has no full crew for the day.</summary>
    Understaffed,

    ForRepair,

    /// <summary>In the shop - Monday's "Repair In Progress" and "Unit Breakdown".</summary>
    UnderRepair,

    /// <summary>Planned upkeep - PMS, change oil, general check-up.</summary>
    Maintenance,

    /// <summary>Held up away from base, e.g. a delivery that ran over into the next day.</summary>
    Stuck,

    ForTransfer,

    /// <summary>Anything else that takes the unit off the road (a confiscated plate, a crew seminar); say what in the notes.</summary>
    Other
}
