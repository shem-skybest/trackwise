using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Reports.Queries;

/// <summary>
/// Query to generate a payroll report for a specified date range.
/// </summary>
public class PayrollReportQuery : PagedIntervalQuery, IQuery<Result<PayrollReportDto>>
{
}
