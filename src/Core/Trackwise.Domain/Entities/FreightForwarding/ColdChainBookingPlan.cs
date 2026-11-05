using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Entities;

/// <summary>What Customer Service fills in for a Cold Chain booking: one truck-trip on one day.</summary>
/// <param name="CallTime">When the truck is due at the client's warehouse, in the tenant's local time.</param>
/// <param name="PickupSiteId">The warehouse the truck loads at. Needed before a trip can be made.</param>
public sealed record ColdChainBookingPlan(
    DateOnly ServiceDate,
    TimeOnly? CallTime,
    TruckTonnageClass TruckSize,
    Guid? PickupSiteId,
    string? Commodity,
    decimal? TemperatureMinC,
    decimal? TemperatureMaxC,
    string? Notes,
    IReadOnlyList<BookingDropSpec> Drops);

public sealed record BookingDropSpec(Guid SiteId, int? PlannedBoxes, int? PlannedBags, string? Notes);
