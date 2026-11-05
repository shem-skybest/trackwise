using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Stats.Queries;

public class GetTrucksStatsListQuery : PagedIntervalQuery, IQuery<PagedResult<TruckStatsDto>>
{
}
