using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// One step of a forwarding workfile's door-to-door timeline. Boxes moved by the same truck event
/// share one entry.
/// </summary>
public class WorkfileTimelineEntryDto
{
    public WorkfileTimelineEventKind Kind { get; set; }
    public DateTime At { get; set; }

    /// <summary>A typed date (ETD, ETA, planned pick-up) rather than a recorded event.</summary>
    public bool IsEstimate { get; set; }

    /// <summary>Still ahead: not counted in any container's status.</summary>
    public bool IsUpcoming { get; set; }

    public string? Place { get; set; }

    public Guid? LegId { get; set; }
    public string? LegReferenceNumber { get; set; }
    public long? TripNumber { get; set; }

    /// <summary>The boxes this step moved; empty on vessel steps, which carry them all.</summary>
    public List<string> Containers { get; set; } = [];
}
