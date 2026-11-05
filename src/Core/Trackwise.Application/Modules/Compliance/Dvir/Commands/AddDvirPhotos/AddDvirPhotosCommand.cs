using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Dvir.Commands;

/// <summary>
/// Photos the crew took of what's broken. The app sends them between create and submit,
/// so they're only accepted while the report is still a draft.
/// </summary>
[RequiresFeature(TenantFeature.Dvir)]
public class AddDvirPhotosCommand : ICommand<Result<DvirReportDto>>
{
    public Guid ReportId { get; set; }
    public List<FileUpload> Files { get; set; } = [];
}
