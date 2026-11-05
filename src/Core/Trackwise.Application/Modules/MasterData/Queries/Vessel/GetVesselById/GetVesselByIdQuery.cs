using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetVesselByIdQuery : IQuery<Result<VesselDto>>, IHaveId
{
    public Guid Id { get; set; }
}
