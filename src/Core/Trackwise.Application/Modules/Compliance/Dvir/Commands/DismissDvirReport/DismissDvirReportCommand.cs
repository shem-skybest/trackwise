using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

/// <summary>
/// Command to dismiss a DVIR report (clears without full review, for DVIRs with no defects)
/// </summary>
[RequiresFeature(TenantFeature.Dvir)]
public record DismissDvirReportCommand : ICommand<Result<DvirReportDto>>
{
    public required Guid ReportId { get; set; }
    public required Guid DismissedById { get; set; }
    public string? Notes { get; set; }
}
