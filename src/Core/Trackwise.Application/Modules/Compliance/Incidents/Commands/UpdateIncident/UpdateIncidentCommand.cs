using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>Rewrites an incident report until it is resolved; every field is sent as it should read.</summary>
public class UpdateIncidentCommand : IncidentFields, ICommand<Result<IncidentDto>>
{
    public Guid Id { get; set; }
}
