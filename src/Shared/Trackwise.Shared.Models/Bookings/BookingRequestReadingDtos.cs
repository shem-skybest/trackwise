using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Shared.Models;

/// <summary>
/// What the AI read from a client's booking request (a Viber screenshot, a pasted Excel table or the
/// message text): one proposed truck-trip per truck asked for. A proposal only - nothing is saved,
/// and Customer Service checks every field before booking.
/// </summary>
public class BookingRequestReadingDto
{
    public List<ProposedColdChainTripDto> Trips { get; set; } = [];

    /// <summary>What the reader could not settle, e.g. a drop name that matches no saved location.</summary>
    public List<string> Warnings { get; set; } = [];
}

/// <summary>One truck the client asked for, shaped like the Cold Chain booking plan.</summary>
public class ProposedColdChainTripDto
{
    public DateOnly? ServiceDate { get; set; }
    public TimeOnly? CallTime { get; set; }
    public TruckTonnageClass? TruckSize { get; set; }

    /// <summary>The saved warehouse the request names, when one matched.</summary>
    public ProposedSiteDto? PickupSite { get; set; }

    /// <summary>The pickup exactly as the client wrote it.</summary>
    public string? PickupText { get; set; }

    public string? Commodity { get; set; }
    public decimal? TemperatureMinC { get; set; }
    public decimal? TemperatureMaxC { get; set; }
    public string? Notes { get; set; }
    public List<ProposedDropDto> Drops { get; set; } = [];
}

public class ProposedDropDto
{
    /// <summary>The drop exactly as the client wrote it ("westin", "batangas1").</summary>
    public string Text { get; set; } = default!;

    /// <summary>The client's saved delivery location it matched, or null when nothing fit.</summary>
    public ProposedSiteDto? Site { get; set; }

    /// <summary>Saved locations it could be, when it matched none for sure. Empty when <see cref="Site"/> is set.</summary>
    public List<ProposedSiteDto> Candidates { get; set; } = [];

    public int? PlannedBoxes { get; set; }
    public int? PlannedBags { get; set; }
    public string? Notes { get; set; }
}

public class ProposedSiteDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public string? City { get; set; }
}
