using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>The approver's disapproval: sends the file back to ops with the reason for rework.</summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class ReturnWorkfileToOpsCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = default!;
}
