using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Reports.Queries;

public class DriverDashboardQuery : PagedIntervalQuery, IQuery<Result<DriverDashboardDto>>
{
}

