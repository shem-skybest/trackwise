using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>Takes a rescue back while none of its drops has a recorded arrival.</summary>
public class UndoTripRescueCommand : ICommand
{
    public Guid TripId { get; set; }
    public Guid RescueId { get; set; }
}
