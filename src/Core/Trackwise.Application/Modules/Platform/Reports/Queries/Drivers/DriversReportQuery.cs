using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Reports.Queries;

public class DriversReportQuery : PagedIntervalQuery, IQuery<PagedResult<DriverReportDto>>
{
    public string? Search { get; set; }
}

