using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

/// <summary>
/// Attaches photos the crew sent in to one block of the crew report. A warehouse visit passes every
/// pickup stop it covers, so each load picked up there gets the photo; a drop passes its one stop.
/// </summary>
public class AddTripStopPhotosCommand : ICommand<Result<List<TripStopPhotoDto>>>
{
    public Guid TripId { get; set; }
    public List<Guid> StopIds { get; set; } = [];
    public List<FileUpload> Photos { get; set; } = [];
}
