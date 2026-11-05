namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// How an agreement adds fuel on top of the base rate ("gas on top"), since diesel moves weekly
/// and the base rate does not.
/// </summary>
public enum FuelAdjustmentMethod
{
    /// <summary>The base rate already covers fuel.</summary>
    None,

    /// <summary>
    /// A fixed amount per diesel price tier, per rate line (e.g. ₱85, ₱100, ₱120 a litre). The tier
    /// is the highest one at or below the diesel price in force on the trip date.
    /// </summary>
    DieselTierSurcharge,

    /// <summary>A percentage of the base rate, set per rate line and revised as diesel moves.</summary>
    Percentage
}
