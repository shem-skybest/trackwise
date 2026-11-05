using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Loads.Queries;

public class GetLoadByIdQuery : IQuery<Result<LoadDto>>, IHaveId
{
    public Guid Id { get; set; }
}
