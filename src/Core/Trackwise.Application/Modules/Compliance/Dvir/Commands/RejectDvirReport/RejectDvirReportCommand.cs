using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

/// <summary>
/// Command to reject a DVIR report (sends back to driver for resubmission)
/// </summary>
[RequiresFeature(TenantFeature.Dvir)]
public record RejectDvirReportCommand : ICommand<Result<DvirReportDto>>
{
    public required Guid ReportId { get; set; }
    public required Guid RejectedById { get; set; }
    public required string RejectionReason { get; set; }
}
