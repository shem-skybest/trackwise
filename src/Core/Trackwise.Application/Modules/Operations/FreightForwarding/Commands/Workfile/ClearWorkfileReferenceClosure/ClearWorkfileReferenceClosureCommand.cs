using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>Accounting's release of a locked reference, which unfreezes the file for ops.</summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class ClearWorkfileReferenceClosureCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string? Remarks { get; set; }
}
