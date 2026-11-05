using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Legs;

/// <summary>What adding and editing a truck leg share.</summary>
public abstract class TruckLegFields
{
    public TruckLegRole Role { get; set; }

    public TruckLegEnd From { get; set; } = new();

    public TruckLegEnd To { get; set; } = new();

    /// <summary>
    /// The parent's container slots this leg moves, one load each. Empty for loose cargo, which
    /// gets a single load with no container.
    /// </summary>
    public List<Guid> ContainerSlotIds { get; set; } = [];

    /// <summary>A subcontract trucker doing the leg instead of our own truck.</summary>
    public Guid? TruckerId { get; set; }

    public DateTime? PickupDate { get; set; }

    public string? Notes { get; set; }
}

/// <summary>One end of a truck leg: a port (terminal) or a saved site. Exactly one is set.</summary>
public class TruckLegEnd
{
    public Guid? TerminalId { get; set; }

    public Guid? SiteId { get; set; }
}
