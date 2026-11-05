using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

/// <summary>
/// Updates non-status fields of a Container. Status transitions go through
/// the dedicated MarkContainer* commands; location-only moves use MoveContainerToTerminal.
/// </summary>
[RequiresFeature(TenantFeature.IntermodalContainers)]
public class UpdateContainerCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string? Number { get; set; }
    public Guid? ContainerTypeId { get; set; }
    public string? Notes { get; set; }
}
