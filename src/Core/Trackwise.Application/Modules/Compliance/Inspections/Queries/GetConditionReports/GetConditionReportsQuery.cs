using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Inspections.Queries;

public class GetConditionReportsQuery : IQuery<Result<List<ConditionReportDto>>>
{
    public Guid? LoadId { get; set; }
    public Guid? ConditionReportId { get; set; }
}
