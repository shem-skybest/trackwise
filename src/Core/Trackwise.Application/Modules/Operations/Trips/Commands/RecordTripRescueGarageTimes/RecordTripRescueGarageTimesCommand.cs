using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// Sets when the rescue truck left its garage and got back, as reported. Both are written as given:
/// a blank time clears it.
/// </summary>
public class RecordTripRescueGarageTimesCommand : ICommand
{
    public Guid TripId { get; set; }
    public Guid RescueId { get; set; }

    /// <summary>With a timezone.</summary>
    public DateTime? DepartedAt { get; set; }

    /// <summary>With a timezone.</summary>
    public DateTime? ArrivedAt { get; set; }
}
