using System.ComponentModel.DataAnnotations.Schema;
using Trackwise.Domain.Core;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;

namespace Trackwise.Domain.Entities;

/// <summary>
/// Represents a freight load to be transported.
/// </summary>
public partial class Load : AuditableEntity, ITenantEntity
{
    public long Number { get; private set; }
    public required string Name { get; set; }
    public required LoadType Type { get; set; }

    public LoadStatus Status { get; private set; } = LoadStatus.Draft;

    public required Address OriginAddress { get; set; }
    public required GeoPoint OriginLocation { get; set; }

    public required Address DestinationAddress { get; set; }
    public required GeoPoint DestinationLocation { get; set; }

    public required Money DeliveryCost { get; set; }

    /// <summary>
    /// Total distance of the load in kilometers.
    /// </summary>
    public double Distance { get; set; }

    /// <summary>
    /// True when the assigned truck is inside the geofence of the next confirmation
    /// checkpoint (pickup if <see cref="Status"/> is <see cref="LoadStatus.Dispatched"/>,
    /// delivery if <see cref="LoadStatus.PickedUp"/>). Toggled by the proximity tracker.
    /// </summary>
    public bool IsInProximity { get; set; }

    /// <summary>
    /// True when this load is return-leg cargo picked up on the way back rather than the outbound
    /// delivery itself. Describes what the cargo <i>is</i>, not how the drop went - a delivery
    /// outcome belongs on <see cref="Status"/> instead.
    /// </summary>
    public bool IsBackload { get; set; }

    /// <summary>What the cargo is, e.g. "Fresh milk". Free text until commodities are master data.</summary>
    public string? Commodity { get; set; }

    /// <summary>Boxes planned for this drop. Null when the load isn't counted in boxes.</summary>
    public int? PlannedBoxes { get; set; }

    /// <summary>Bags planned for this drop. Null when the load isn't counted in bags.</summary>
    public int? PlannedBags { get; set; }

    /// <summary>
    /// Boxes the consignee refused and that came back on the truck - a partial delivery. Recorded at
    /// the drop's departure; delivered is planned minus returned.
    /// </summary>
    public int? ReturnedBoxes { get; set; }

    /// <summary>Bags that came back on the truck, like <see cref="ReturnedBoxes" />.</summary>
    public int? ReturnedBags { get; set; }

    /// <summary>Why anything came back. Set whenever <see cref="ReturnedBoxes" /> or <see cref="ReturnedBags" /> is.</summary>
    public DropReturnReason? ReturnReason { get; set; }

    /// <summary>
    /// What happened at the drop in the staff's words - the reason behind a foul trip, a
    /// client cancellation or a partial delivery.
    /// </summary>
    public string? OutcomeNote { get; set; }

    /// <summary>
    /// Computed: true when the driver can confirm pickup right now.
    /// </summary>
    [NotMapped]
    public bool CanConfirmPickUp => Status == LoadStatus.Dispatched && IsInProximity;

    /// <summary>
    /// Computed: true when the driver can confirm delivery right now.
    /// </summary>
    [NotMapped]
    public bool CanConfirmDelivery => Status == LoadStatus.PickedUp && IsInProximity;

    public DateTime? DispatchedAt { get; private set; }
    public DateTime? PickedUpAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public Guid CustomerId { get; set; }
    public virtual required Customer Customer { get; set; }

    public Guid? AssignedTruckId { get; set; }
    public virtual Truck? AssignedTruck { get; set; }

    public Guid? AssignedDispatcherId { get; set; }
    public virtual Employee? AssignedDispatcher { get; set; }

    public virtual LoadInvoice? Invoice { get; set; }

    public virtual List<LoadDocument> Documents { get; set; } = [];
    public virtual List<LoadException> Exceptions { get; set; } = [];
    public virtual ICollection<TripStop> TripStops { get; } = [];

    /// <summary>
    /// How this load entered the system (manual entry, email/PDF import, API, load board).
    /// </summary>
    public LoadSource Source { get; set; } = LoadSource.Manual;

    /// <summary>
    /// Customer-requested pickup window date.
    /// </summary>
    public DateTime? RequestedPickupDate { get; set; }

    /// <summary>
    /// Customer-requested delivery window date.
    /// </summary>
    public DateTime? RequestedDeliveryDate { get; set; }

    /// <summary>
    /// Free-form dispatcher / driver notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// True when the load contains hazardous materials. When set, <see cref="HazmatClass"/>
    /// and <see cref="UnNumber"/> should also be populated.
    /// </summary>
    public bool IsHazmat { get; set; }

    /// <summary>
    /// UN/ADR hazardous material class (1–9). Required for ADR-aware dispatch eligibility.
    /// </summary>
    public HazmatClass? HazmatClass { get; set; }

    /// <summary>
    /// UN number identifying the specific hazardous substance (e.g., "UN1203" for petrol).
    /// </summary>
    public string? UnNumber { get; set; }

    /// <summary>
    /// Optional intermodal container being moved by this load.
    /// </summary>
    public Guid? ContainerId { get; set; }

    public virtual Container? Container { get; set; }

    /// <summary>
    /// Optional origin terminal (port / rail / depot) - when set, the truck picks up here.
    /// <see cref="OriginAddress"/> is still populated as a denormalized snapshot.
    /// </summary>
    public Guid? OriginTerminalId { get; set; }

    public virtual Terminal? OriginTerminal { get; set; }

    /// <summary>
    /// Optional destination terminal - when set, the truck drops off here.
    /// </summary>
    public Guid? DestinationTerminalId { get; set; }

    public virtual Terminal? DestinationTerminal { get; set; }

    /// <summary>
    /// The saved site (warehouse, outlet) the load is picked up from, when it was chosen from the
    /// customer's list rather than typed in. <see cref="OriginAddress"/> stays a snapshot, so
    /// editing the site later never rewrites where a past load went.
    /// </summary>
    public Guid? OriginSiteId { get; set; }

    public virtual Location? OriginSite { get; set; }

    /// <summary>The saved site the load is dropped at. See <see cref="OriginSiteId"/>.</summary>
    public Guid? DestinationSiteId { get; set; }

    public virtual Location? DestinationSite { get; set; }

    /// <summary>
    /// If the load was booked from a load board, the provider type.
    /// </summary>
    public LoadBoardProviderType? ExternalSourceProvider { get; set; }

    /// <summary>
    /// External listing ID from the load board provider.
    /// </summary>
    public string? ExternalSourceId { get; set; }

    /// <summary>
    /// Broker reference number from the load board.
    /// </summary>
    public string? ExternalBrokerReference { get; set; }

    /// <summary>
    /// Optional freight-forwarding Booking this Load executes. Nullable for backward compatibility -
    /// Loads created before the Bookings module existed have no Booking to point to. Going forward,
    /// a Load can only be created against a Booking that is Confirmed or InExecution.
    /// </summary>
    public Guid? BookingId { get; set; }

    public virtual Booking? Booking { get; set; }

    /// <summary>
    /// The truck leg (<see cref="TruckingWorkfile.IsLeg"/>) this load is part of. A leg's loads
    /// carry no invoice: the move is billed once, on the forwarding workfile.
    /// </summary>
    public Guid? WorkfileId { get; set; }

    /// <summary>
    /// The forwarding workfile's container slot this load moves. The slot, not the box, because
    /// the box's number usually isn't known yet when the leg is planned - it is read through the
    /// slot once someone types it.
    /// </summary>
    public Guid? WorkfileContainerId { get; set; }

    public decimal CalcDriverShare()
    {
        return DeliveryCost * (decimal)(AssignedTruck?.GetDriversShareRatio() ?? 0);
    }
}
