using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

[RequiresFeature(TenantFeature.Dvir)]
public record SubmitDvirReportCommand : ICommand<Result<DvirReportDto>>
{
    public required Guid ReportId { get; set; }
    public string? DriverSignature { get; set; }
}
