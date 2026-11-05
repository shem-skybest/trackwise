using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>Records when the truck left the garage for the trip and/or got back, as reported.</summary>
public class RecordTripGarageTimesCommand : ICommand
{
    public Guid TripId { get; set; }

    /// <summary>When the truck left the garage, with a timezone. Omit to leave it unchanged.</summary>
    public DateTime? DepartedAt { get; set; }

    /// <summary>When the truck got back to the garage, with a timezone. Omit to leave it unchanged.</summary>
    public DateTime? ArrivedAt { get; set; }
}
