using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public class LoadDto
{
    public Guid Id { get; set; }
    public long Number { get; set; }
    public string? Name { get; set; }
    public required Address OriginAddress { get; set; }
    public required GeoPoint OriginLocation { get; set; }
    public required Address DestinationAddress { get; set; }
    public required GeoPoint DestinationLocation { get; set; }
    public decimal DeliveryCost { get; set; }
    public double Distance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool CanConfirmPickUp { get; set; }
    public bool CanConfirmDelivery { get; set; }
    public bool IsInProximity { get; set; }
    public LoadStatus Status { get; set; }
    public LoadType Type { get; set; }
    public LoadSource Source { get; set; }
    public DateTime? RequestedPickupDate { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }

    public string? Commodity { get; set; }
    public int? PlannedBoxes { get; set; }
    public int? PlannedBags { get; set; }
    public int? ReturnedBoxes { get; set; }
    public int? ReturnedBags { get; set; }
    public DropReturnReason? ReturnReason { get; set; }
    public string? OutcomeNote { get; set; }
    public bool IsHazmat { get; set; }
    public HazmatClass? HazmatClass { get; set; }
    public string? UnNumber { get; set; }
    public Guid? ContainerId { get; set; }
    public string? ContainerNumber { get; set; }
    public string? ContainerTypeCode { get; set; }
    public Guid? OriginTerminalId { get; set; }
    public string? OriginTerminalName { get; set; }
    public string? OriginTerminalCode { get; set; }
    public Guid? DestinationTerminalId { get; set; }
    public string? DestinationTerminalName { get; set; }
    public string? DestinationTerminalCode { get; set; }

    /// <summary>Saved pickup site, when one was picked. Resolve it by id for its name and cut-off.</summary>
    public Guid? OriginSiteId { get; set; }

    public Guid? DestinationSiteId { get; set; }
    public Guid? AssignedDispatcherId { get; set; }
    public string? AssignedDispatcherName { get; set; }
    public Guid? AssignedTruckId { get; set; }
    public string? AssignedTruckNumber { get; set; }
    public Address? CurrentAddress { get; set; }
    public GeoPoint? CurrentLocation { get; set; }
    public CustomerDto? Customer { get; set; }

    public Guid? TripId { get; set; }
    public long? TripNumber { get; set; }
    public string? TripName { get; set; }

    public IEnumerable<string>? AssignedTruckDriversName { get; set; }
    public InvoiceDto? Invoice { get; set; }
}
