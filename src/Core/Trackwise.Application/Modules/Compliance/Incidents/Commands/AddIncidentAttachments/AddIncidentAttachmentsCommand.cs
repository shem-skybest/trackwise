using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>Attaches photos the crew sent, the police report or blotter, or any other paper to an incident.</summary>
public class AddIncidentAttachmentsCommand : ICommand<Result<IncidentDto>>
{
    public Guid IncidentId { get; set; }

    /// <summary><see cref="DocumentType.Photo" />, <see cref="DocumentType.PoliceReport" /> or <see cref="DocumentType.Other" />.</summary>
    public DocumentType Type { get; set; } = DocumentType.Photo;

    public List<FileUpload> Files { get; set; } = [];
}
