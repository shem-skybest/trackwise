using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Containers.Queries;

[RequiresFeature(TenantFeature.IntermodalContainers)]
public class GetContainerByIdQuery : IQuery<Result<ContainerDto>>, IHaveId
{
    public Guid Id { get; set; }
}
