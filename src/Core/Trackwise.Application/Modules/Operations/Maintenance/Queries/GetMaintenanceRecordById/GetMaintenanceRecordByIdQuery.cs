using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

public class GetMaintenanceRecordByIdQuery : IQuery<Result<MaintenanceRecordDto>>, IHaveId
{
    public Guid Id { get; set; }
}
