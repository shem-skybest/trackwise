using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

/// <summary>Nested load payload for trip create/update; not a MediatR command.</summary>
public record CreateTripLoadCommand
{
    public string? TempId { get; set; } // Temporary ID for mapping optimized stops

    public required string Name { get; set; }

    public required Address OriginAddress { get; set; }
    public required GeoPoint OriginLocation { get; set; }

    public required Address DestinationAddress { get; set; }
    public required GeoPoint DestinationLocation { get; set; }

    public decimal DeliveryCost { get; set; }
    public double Distance { get; set; }
    public LoadType Type { get; set; }
    public required Guid AssignedDispatcherId { get; set; }
    public required Guid CustomerId { get; set; }

    public DateTime? RequestedPickupDate { get; set; }

    /// <summary>The drop's cut-off: deliver by this date and time. Arriving later counts as late.</summary>
    public DateTime? RequestedDeliveryDate { get; set; }

    public string? Notes { get; set; }

    /// <summary>What the cargo is, e.g. "Fresh milk".</summary>
    public string? Commodity { get; set; }

    public int? PlannedBoxes { get; set; }
    public int? PlannedBags { get; set; }

    /// <summary>Saved pickup site; must be the customer's own or shared.</summary>
    public Guid? OriginSiteId { get; set; }

    /// <summary>Saved drop site; same rule as the origin.</summary>
    public Guid? DestinationSiteId { get; set; }

    /// <summary>The booking this load executes - must be Confirmed or InExecution.</summary>
    public Guid? BookingId { get; set; }
}
