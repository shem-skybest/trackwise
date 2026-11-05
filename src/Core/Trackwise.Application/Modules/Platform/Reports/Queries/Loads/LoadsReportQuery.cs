using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Reports.Queries;

public class LoadsReportQuery : PagedIntervalQuery, IQuery<Result<LoadsReportDto>>
{
}

