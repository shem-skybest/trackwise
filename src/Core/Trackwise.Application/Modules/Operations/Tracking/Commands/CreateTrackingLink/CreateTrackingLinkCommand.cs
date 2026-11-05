using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Tracking.Commands;

public class CreateTrackingLinkCommand : ICommand<Result<TrackingLinkDto>>
{
    public required Guid LoadId { get; set; }
}
