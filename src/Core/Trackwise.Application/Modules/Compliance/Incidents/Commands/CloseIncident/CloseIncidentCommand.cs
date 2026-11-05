using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>Closes a resolved incident for good.</summary>
public class CloseIncidentCommand : ICommand<Result<IncidentDto>>
{
    public Guid Id { get; set; }
}
