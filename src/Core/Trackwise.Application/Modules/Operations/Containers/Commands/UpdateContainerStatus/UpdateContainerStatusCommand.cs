using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

/// <summary>
/// Transitions a container to a target lifecycle status.
/// <see cref="TerminalId"/> is required for transitions to <c>AtPort</c> and <c>Returned</c>;
/// ignored for other targets.
/// </summary>
[RequiresFeature(TenantFeature.IntermodalContainers)]
public class UpdateContainerStatusCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public ContainerStatus TargetStatus { get; set; }
    public Guid? TerminalId { get; set; }
}
