using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Tracking.Commands;

public class RevokeTrackingLinkCommand : ICommand<Result>
{
    public required Guid Id { get; set; }
}
