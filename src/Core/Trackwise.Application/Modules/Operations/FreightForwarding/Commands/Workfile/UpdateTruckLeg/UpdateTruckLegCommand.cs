using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Application.Modules.Operations.FreightForwarding.Legs;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Edits a truck leg before any of its loads goes out: its role, ends, boxes, trucker, date and
/// notes. Boxes added get a new load; boxes taken off lose theirs.
/// </summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class UpdateTruckLegCommand : TruckLegFields, ICommand<Result>
{
    /// <summary>The forwarding workfile the leg belongs to.</summary>
    public Guid WorkfileId { get; set; }

    /// <summary>The leg's own workfile id.</summary>
    public Guid LegId { get; set; }
}
