using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>Reopens a resolved incident, e.g. when the client comes back about it.</summary>
public class ReopenIncidentCommand : ICommand<Result<IncidentDto>>
{
    public Guid Id { get; set; }
    public string Reason { get; set; } = "";
}
