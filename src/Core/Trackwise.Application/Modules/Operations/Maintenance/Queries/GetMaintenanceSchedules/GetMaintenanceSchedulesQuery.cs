using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

public class GetMaintenanceSchedulesQuery : IQuery<Result<List<MaintenanceScheduleDto>>>
{
    public Guid? TruckId { get; set; }
    public bool IncludeInactive { get; set; }
}
