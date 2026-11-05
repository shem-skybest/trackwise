namespace Trackwise.Domain.Primitives.Enums;

/// <summary>
/// Why a booking didn't run - the reasons Cold Chain's booking board records against cancelled and
/// lost bookings. A pick list rather than free text so "lack of personnel" is countable per month.
/// </summary>
public enum BookingLossReason
{
    LackOfPersonnel = 0,
    LackOfUnit = 1,
    LackOfDocuments = 2,

    /// <summary>Crew away for NMIS inspection, disaster or other training.</summary>
    CrewInTrainingOrInspection = 3,

    ClientBackedOut = 4,

    /// <summary>The client's own customer cancelled the order.</summary>
    EndCustomerCancelled = 5,

    ClientUsedOwnUnit = 6,

    /// <summary>The client hasn't accepted a rate, e.g. a proposed fuel increase.</summary>
    RateNotAgreed = 7,

    /// <summary>Flooded area, typhoon, closed road.</summary>
    WeatherOrRoadCondition = 8,

    Other = 9
}
