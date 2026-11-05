using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Maintenance.Queries;

public class GetMaintenanceRequisitionQuery : IQuery<Result<MaintenanceRequisitionDto>>
{
    public required string RequisitionNumber { get; set; }
}
