using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Eld.Queries;

public class GetDriverHosLogsQuery : PagedIntervalQuery, IQuery<PagedResult<HosLogDto>>
{
    public Guid EmployeeId { get; set; }
}
