using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetBranchByIdQuery : IQuery<Result<BranchDto>>, IHaveId
{
    public Guid Id { get; set; }
}
