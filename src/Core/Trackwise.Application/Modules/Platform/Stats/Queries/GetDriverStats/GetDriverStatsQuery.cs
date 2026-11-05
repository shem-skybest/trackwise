using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Stats.Queries;

public class GetDriverStatsQuery : IQuery<Result<DriverStatsDto>>
{
    public Guid UserId { get; set; }
}
