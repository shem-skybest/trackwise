namespace Trackwise.Domain.Primitives.Enums;

/// <summary>How often a client is billed - each Cold Chain client agrees its own cut-off.</summary>
public enum BillingCycle
{
    /// <summary>The 1st to the 15th, then the 16th to month end.</summary>
    SemiMonthly = 0,

    /// <summary>Monday to Saturday.</summary>
    Weekly = 1,

    /// <summary>One billing per trip, for occasional and genset clients.</summary>
    PerTrip = 2,

    Monthly = 3
}
