using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

/// <summary>
/// Pure location update - moves the container to a terminal without changing its lifecycle status.
/// </summary>
[RequiresFeature(TenantFeature.IntermodalContainers)]
public class MoveContainerToTerminalCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public Guid TerminalId { get; set; }
}
