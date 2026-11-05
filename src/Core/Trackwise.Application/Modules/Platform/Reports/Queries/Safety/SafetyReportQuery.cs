using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Reports.Queries;

public class SafetyReportQuery : PagedIntervalQuery, IQuery<Result<SafetyReportDto>>
{
}
