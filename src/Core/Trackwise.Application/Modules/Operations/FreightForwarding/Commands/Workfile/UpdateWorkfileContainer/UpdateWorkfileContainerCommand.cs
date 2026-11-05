using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Application.Modules.Operations.FreightForwarding.Containers;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>Edits one container slot - typically filling in the box number and seal once known.</summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class UpdateWorkfileContainerCommand : WorkfileContainerFields, ICommand<Result>
{
    public Guid WorkfileId { get; set; }

    /// <summary>The slot's id on the workfile, not the box's id in the register.</summary>
    public Guid Id { get; set; }
}
