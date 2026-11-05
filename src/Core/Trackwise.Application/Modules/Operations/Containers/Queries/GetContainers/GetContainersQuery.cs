using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Queries;

[RequiresFeature(TenantFeature.IntermodalContainers)]
public class GetContainersQuery : SearchableQuery, IQuery<PagedResult<ContainerDto>>
{
    public ContainerStatus? Status { get; set; }
    public Guid? ContainerTypeId { get; set; }
    public Guid? CurrentTerminalId { get; set; }
}
