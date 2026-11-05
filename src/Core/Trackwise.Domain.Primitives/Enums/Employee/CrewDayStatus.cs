namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Where a driver or helper stands on one day of the crew plan. On Monday's CCL OPS - DISPATCH board
/// this was typed into the person's name as a suffix ("ANDRES ( ABSENT )", "BENITO - ON LEAVE");
/// here it is a <c>CrewStatusEntry</c>. No entry for the day means <see cref="Available" />.
/// </summary>
public enum CrewDayStatus
{
    Available,

    /// <summary>On standby for the day - Monday's "RESERVE" / "RESERVED", usually riding a buffer unit.</summary>
    Reserve,

    /// <summary>Didn't report for work, including refusing the day's dispatch.</summary>
    Absent,

    OnLeave,

    Suspended,

    /// <summary>At a seminar or training for the day, e.g. NMIS.</summary>
    Training,

    /// <summary>Working another unit for the day - Monday's "ASSIGNED TO WINGVAN".</summary>
    LentOut
}

public static class CrewDayStatusExtensions
{
    /// <summary>
    /// Statuses the crew plan and trips refuse: the person isn't working that day at all. An array,
    /// so queries can filter on it.
    /// </summary>
    public static readonly CrewDayStatus[] Blocking =
        [CrewDayStatus.Absent, CrewDayStatus.OnLeave, CrewDayStatus.Suspended];

    /// <summary>
    /// Statuses the crew pickers offer. Reserve crew are in - standby is what they're for. Training
    /// and lent-out crew are elsewhere that day, so they're left out, but not refused outright.
    /// </summary>
    public static readonly CrewDayStatus[] Offered = [CrewDayStatus.Available, CrewDayStatus.Reserve];

    public static bool BlocksAssignment(this CrewDayStatus status) => Blocking.Contains(status);

    public static bool IsOfferedForAssignment(this CrewDayStatus status) => Offered.Contains(status);
}
