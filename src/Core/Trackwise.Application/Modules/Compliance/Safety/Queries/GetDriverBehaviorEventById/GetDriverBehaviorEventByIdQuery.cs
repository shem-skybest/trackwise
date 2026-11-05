using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Safety.Queries;

public class GetDriverBehaviorEventByIdQuery : IQuery<Result<DriverBehaviorEventDto>>, IHaveId
{
    public Guid Id { get; set; }
}
