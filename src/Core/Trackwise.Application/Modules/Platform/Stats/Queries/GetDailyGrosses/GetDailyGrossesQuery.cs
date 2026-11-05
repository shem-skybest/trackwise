using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Stats.Queries;

public class GetDailyGrossesQuery : IntervalQuery, IQuery<Result<DailyGrossesDto>>
{
    public Guid? TruckId { get; set; }
    public Guid? UserId { get; set; }
}
