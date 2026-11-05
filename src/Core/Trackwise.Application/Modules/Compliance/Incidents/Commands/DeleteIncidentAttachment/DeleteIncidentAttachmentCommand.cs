using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>Removes a file attached to an incident, e.g. a photo put on the wrong case.</summary>
public class DeleteIncidentAttachmentCommand : ICommand<Result<IncidentDto>>
{
    public Guid IncidentId { get; set; }
    public Guid AttachmentId { get; set; }
}
