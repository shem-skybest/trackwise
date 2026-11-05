using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

public class CreateLoadCommand : ICommand
{
    public string Name { get; set; } = null!;
    public LoadType Type { get; set; }
    public Address OriginAddress { get; set; } = null!;
    public GeoPoint OriginLocation { get; set; } = null!;
    public Address DestinationAddress { get; set; } = null!;
    public GeoPoint DestinationLocation { get; set; } = null!;
    public decimal DeliveryCost { get; set; }
    public double Distance { get; set; }
    public Guid AssignedDispatcherId { get; set; }
    public Guid? AssignedTruckId { get; set; }
    public Guid CustomerId { get; set; }

    public LoadSource Source { get; set; } = LoadSource.Manual;
    public DateTime? RequestedPickupDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public Guid? ContainerId { get; set; }
    public Guid? OriginTerminalId { get; set; }
    public Guid? DestinationTerminalId { get; set; }

    /// <summary>Saved pickup site. The address above is still stored as the load's own copy.</summary>
    public Guid? OriginSiteId { get; set; }

    /// <summary>Saved drop site - one of the customer's outlets, or a shared site.</summary>
    public Guid? DestinationSiteId { get; set; }
    public Guid? BookingId { get; set; }

    public bool IsHazmat { get; set; }
    public HazmatClass? HazmatClass { get; set; }
    public string? UnNumber { get; set; }

    /// <summary>What the cargo is, e.g. "Fresh milk".</summary>
    public string? Commodity { get; set; }

    /// <summary>Boxes planned for this drop.</summary>
    public int? PlannedBoxes { get; set; }

    /// <summary>Bags planned for this drop.</summary>
    public int? PlannedBags { get; set; }
}
