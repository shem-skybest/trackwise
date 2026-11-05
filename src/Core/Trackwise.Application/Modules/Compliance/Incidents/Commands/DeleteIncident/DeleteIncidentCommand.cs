using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Incidents.Commands;

/// <summary>Deletes a report made by mistake. Linked repairs and rescues keep their records and lose the link.</summary>
public class DeleteIncidentCommand : ICommand
{
    public Guid Id { get; set; }
}
