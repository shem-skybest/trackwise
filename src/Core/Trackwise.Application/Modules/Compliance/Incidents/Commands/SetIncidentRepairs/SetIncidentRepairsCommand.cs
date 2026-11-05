using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>
/// The repairs the incident caused, as the whole set: records left out are unlinked. A repair belongs
/// to one incident at most, and must be on the incident's truck when it has one.
/// </summary>
public class SetIncidentRepairsCommand : ICommand<Result<IncidentDto>>
{
    public Guid Id { get; set; }
    public List<Guid> RepairIds { get; set; } = [];
}
