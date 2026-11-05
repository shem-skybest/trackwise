using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

/// <summary>Clears a truck-day back to unplanned. Trips already created from it keep their crew.</summary>
public class DeleteCrewAssignmentCommand : ICommand<Result>
{
    public Guid Id { get; set; }
}
