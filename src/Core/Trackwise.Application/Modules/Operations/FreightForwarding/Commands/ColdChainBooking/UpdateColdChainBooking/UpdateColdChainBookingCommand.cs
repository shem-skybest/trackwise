using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Changes the plan of a Cold Chain booking that hasn't started, and re-estimates it. A new service
/// date may fall under a different revision of the client's agreement.
/// </summary>
public class UpdateColdChainBookingCommand : ColdChainBookingPlanRequest, ICommand<Result>
{
    public Guid Id { get; set; }
}
