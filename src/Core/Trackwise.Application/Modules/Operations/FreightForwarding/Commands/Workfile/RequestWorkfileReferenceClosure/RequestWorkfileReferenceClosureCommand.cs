using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Ops asks Accounting to close the reference. This freezes the file wherever it sits in the
/// lifecycle; ops cannot undo it - that handshake is the point.
/// </summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class RequestWorkfileReferenceClosureCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
