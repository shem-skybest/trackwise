using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Commands;

[RequiresFeature(TenantFeature.IntermodalContainers)]
public class CreateContainerCommand : ICommand<Result<ContainerDto>>
{
    public string Number { get; set; } = null!;
    public Guid ContainerTypeId { get; set; }
    public bool IsLaden { get; set; }
    public ContainerStatus Status { get; set; } = ContainerStatus.Empty;
    public Guid? CurrentTerminalId { get; set; }
    public string? Notes { get; set; }
}
