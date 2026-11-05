using System.ComponentModel;

namespace Trackwise.Domain.Primitives.Enums.Safety;

/// <summary>
/// What went wrong, from the cases Cold Chain logs in its weekly report (<c>CCL-CASENO.###</c>).
/// A backload is also a drop's outcome on the trip; it becomes a case when it needs follow-up.
/// </summary>
public enum IncidentKind
{
    /// <summary>A road accident or the truck hitting something.</summary>
    Accident,

    /// <summary>The truck or its reefer unit broke down on the job.</summary>
    Breakdown,

    TrafficViolation,

    [Description("Product damage or quality")]
    ProductDamage,

    TemperatureExcursion,

    /// <summary>Short-loaded, wrong items loaded, or a wrong serve at the outlet.</summary>
    [Description("Loading error")]
    LoadingError,

    WrongDrop,

    /// <summary>Delivery papers missing, wrong or not brought back.</summary>
    Documents,

    [Description("Theft or missing goods")]
    Theft,

    Other,

    /// <summary>Goods brought back from a drop - expired P.O., rejected, no one to receive.</summary>
    Backload,

    /// <summary>Late at the warehouse or the outlet, or kept waiting for hours.</summary>
    Delay,

    /// <summary>A booking or trip called off.</summary>
    Cancellation,

    /// <summary>Not enough trucks or crew for the day's bookings.</summary>
    [Description("No truck or crew")]
    ResourceShortage
}
