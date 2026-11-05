using Trackwise.Application.Abstractions;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>Corrects the recorded garage times, each with a timezone, plus why.</summary>
public class CorrectTripGarageTimesCommand : ICommand
{
    public Guid TripId { get; set; }

    /// <summary>The garage departure as it should read; omit only if none is recorded.</summary>
    public DateTime? DepartedAt { get; set; }

    /// <summary>The garage arrival as it should read; omit only if none is recorded.</summary>
    public DateTime? ArrivedAt { get; set; }

    /// <summary>Why the recorded times are being changed. Required.</summary>
    public string Reason { get; set; } = "";
}
