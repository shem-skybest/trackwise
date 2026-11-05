using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

[RequiresFeature(TenantFeature.Dvir)]
public record ReviewDvirReportCommand : ICommand<Result<DvirReportDto>>
{
    public required Guid ReportId { get; set; }
    public required Guid ReviewedById { get; set; }
    public required bool DefectsCorrected { get; set; }
    public string? MechanicSignature { get; set; }
    public string? MechanicNotes { get; set; }
}
