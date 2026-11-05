using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

[RequiresFeature(TenantFeature.IntermodalContainers)]
public class DeleteContainerCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
