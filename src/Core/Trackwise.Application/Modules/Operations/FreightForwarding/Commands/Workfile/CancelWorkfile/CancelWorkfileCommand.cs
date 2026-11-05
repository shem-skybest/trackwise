using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

[RequiresFeature(TenantFeature.Workfiles)]
public class CancelWorkfileCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string CancelledBy { get; set; } = default!;
    public string Reason { get; set; } = default!;
}
