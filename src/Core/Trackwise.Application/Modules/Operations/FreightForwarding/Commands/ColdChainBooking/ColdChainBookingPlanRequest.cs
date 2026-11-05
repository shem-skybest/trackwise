using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>The trip plan a Cold Chain booking is created, edited or estimated with.</summary>
public abstract class ColdChainBookingPlanRequest
{
    /// <summary>The day the trip runs, in the tenant's local calendar.</summary>
    public DateOnly ServiceDate { get; set; }

    /// <summary>When the truck is due at the pickup warehouse, local time.</summary>
    public TimeOnly? CallTime { get; set; }

    public TruckTonnageClass TruckSize { get; set; }
    public Guid? PickupSiteId { get; set; }
    public string? Commodity { get; set; }
    public decimal? TemperatureMinC { get; set; }
    public decimal? TemperatureMaxC { get; set; }
    public string? Notes { get; set; }
    public List<BookingDropInput> Drops { get; set; } = [];

    public ColdChainBookingPlan ToPlan() => new(
        ServiceDate,
        CallTime,
        TruckSize,
        PickupSiteId,
        Commodity,
        TemperatureMinC,
        TemperatureMaxC,
        Notes,
        Drops.Select(d => new BookingDropSpec(d.SiteId, d.PlannedBoxes, d.PlannedBags, d.Notes)).ToList());
}
