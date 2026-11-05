using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Reports.Queries;

[RequiresFeature(TenantFeature.Ifta)]
public class IftaReportQuery : IQuery<Result<IftaReportDto>>
{
    public int Year { get; set; }

    /// <summary>Calendar quarter, 1-4.</summary>
    public int Quarter { get; set; }
}
