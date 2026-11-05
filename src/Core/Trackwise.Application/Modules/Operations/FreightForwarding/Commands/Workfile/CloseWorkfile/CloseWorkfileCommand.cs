using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

[RequiresFeature(TenantFeature.Workfiles)]
public class CloseWorkfileCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string ClosedBy { get; set; } = default!;

    /// <summary>Accounting's note on the file, recorded alongside the close.</summary>
    public string? Remarks { get; set; }
}
