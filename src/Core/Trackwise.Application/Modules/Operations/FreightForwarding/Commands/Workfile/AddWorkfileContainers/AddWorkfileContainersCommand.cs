using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Application.Modules.Operations.FreightForwarding.Containers;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Adds container slots to a workfile - "4 x 40HC" before any box has a number, or one numbered box.
/// </summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class AddWorkfileContainersCommand : WorkfileContainerFields, ICommand<Result>
{
    public Guid WorkfileId { get; set; }

    /// <summary>How many slots of this size. Must be 1 when a container number is given.</summary>
    public int Quantity { get; set; } = 1;
}
