using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>Removes a slot from the workfile. The box itself stays in the container register.</summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class RemoveWorkfileContainerCommand : ICommand<Result>
{
    public Guid WorkfileId { get; set; }
    public Guid Id { get; set; }
}
