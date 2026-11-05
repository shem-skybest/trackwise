using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>A truck leg as its forwarding workfile lists it.</summary>
public class TruckLegDto
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = default!;
    public TruckLegRole Role { get; set; }
    public WorkfileStatus Status { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? TruckerName { get; set; }
    public DateTime? PickupDate { get; set; }

    /// <summary>The boxes it moves - number, or size and slot when the number isn't known yet.</summary>
    public List<string> Containers { get; set; } = [];

    public Guid? FromTerminalId { get; set; }
    public Guid? FromSiteId { get; set; }
    public Guid? ToTerminalId { get; set; }
    public Guid? ToSiteId { get; set; }
    public Guid? TruckerId { get; set; }
    public List<Guid> ContainerSlotIds { get; set; } = [];
    public string? Notes { get; set; }

    /// <summary>Open, and none of its loads dispatched or put on a trip yet.</summary>
    public bool CanEdit { get; set; }
}

/// <summary>One load of a truck leg.</summary>
public class TruckLegLoadDto
{
    public Guid Id { get; set; }
    public long Number { get; set; }
    public string Name { get; set; } = default!;
    public LoadStatus Status { get; set; }
    public string? Container { get; set; }
    public string? TruckNumber { get; set; }
}
