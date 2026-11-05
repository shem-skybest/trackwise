using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

[RequiresFeature(TenantFeature.IntermodalContainers)]
public class LinkContainerToLoadCommand : ICommand<Result>
{
    public Guid ContainerId { get; set; }
    public Guid LoadId { get; set; }
}
