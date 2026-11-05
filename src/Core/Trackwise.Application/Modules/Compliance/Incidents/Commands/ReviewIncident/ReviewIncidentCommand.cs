using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>Puts an open incident under review, e.g. while the investigation decides on a crew charge.</summary>
public class ReviewIncidentCommand : ICommand<Result<IncidentDto>>
{
    public Guid Id { get; set; }
    public string? Notes { get; set; }
}
