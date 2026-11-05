using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>Resolves an incident with what was done about it.</summary>
public class ResolveIncidentCommand : ICommand<Result<IncidentDto>>
{
    public Guid Id { get; set; }
    public string CorrectiveAction { get; set; } = "";
}
