using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Shared.Models;

public record TripLoadDto
{
    public Guid Id { get; set; }
    public long Number { get; set; }
    public required string Name { get; set; }
    public LoadStatus Status { get; set; }
    public LoadType Type { get; set; }
    public double Distance { get; set; }
    public decimal DeliveryCost { get; set; }
    public required Address OriginAddress { get; set; }
    public required GeoPoint OriginLocation { get; set; }
    public required Address DestinationAddress { get; set; }
    public required GeoPoint DestinationLocation { get; set; }
    public required CustomerDto Customer { get; set; }
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
}
