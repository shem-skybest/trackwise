using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

public class UpdateLoadCommand : ICommand
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public LoadType? Type { get; set; }
    public Address? OriginAddress { get; set; }
    public GeoPoint? OriginLocation { get; set; }
    public Address? DestinationAddress { get; set; }
    public GeoPoint? DestinationLocation { get; set; }
    public decimal? DeliveryCost { get; set; }
    public double? Distance { get; set; }
    public Guid? AssignedDispatcherId { get; set; }
    public Guid? AssignedTruckId { get; set; }
    public Guid? CustomerId { get; set; }
    public LoadStatus? Status { get; set; }

    public LoadSource? Source { get; set; }
    public DateTime? RequestedPickupDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
    public Guid? ContainerId { get; set; }
    public Guid? OriginTerminalId { get; set; }
    public Guid? DestinationTerminalId { get; set; }

    /// <summary>
    /// Saved pickup site. Unlike the other optional fields this is always applied, so null
    /// unlinks: typing a new address over a picked site has to be able to clear it.
    /// </summary>
    public Guid? OriginSiteId { get; set; }

    /// <summary>Saved drop site. Always applied, like <see cref="OriginSiteId"/>.</summary>
    public Guid? DestinationSiteId { get; set; }

    public bool? IsHazmat { get; set; }
    public HazmatClass? HazmatClass { get; set; }
    public string? UnNumber { get; set; }

    /// <summary>What the cargo is, e.g. "Fresh milk".</summary>
    public string? Commodity { get; set; }

    /// <summary>Boxes planned for this drop.</summary>
    public int? PlannedBoxes { get; set; }

    /// <summary>Bags planned for this drop.</summary>
    public int? PlannedBags { get; set; }
}
