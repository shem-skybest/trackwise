using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Application.Modules.Operations.FreightForwarding.Legs;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Opens a truck leg (a child SLC-TRK trucking workfile) under a forwarding workfile, with one load
/// per container slot it moves. Returns the leg's id.
/// </summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class AddTruckLegCommand : TruckLegFields, ICommand<Result<Guid>>
{
    public Guid WorkfileId { get; set; }
}
