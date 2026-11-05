using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetContainerTypeByIdQuery : IQuery<Result<ContainerTypeDto>>, IHaveId
{
    public Guid Id { get; set; }
}
