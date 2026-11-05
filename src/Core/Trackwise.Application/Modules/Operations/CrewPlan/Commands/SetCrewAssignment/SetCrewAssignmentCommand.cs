using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

/// <summary>
/// Plans one truck for one day: its status and crew. Replaces that truck-day's row if there is one,
/// so the plan screen saves a row the same way whether or not it was planned before.
/// </summary>
public class SetCrewAssignmentCommand : ICommand<Result<Guid>>
{
    public DateOnly Date { get; set; }
    public Guid TruckId { get; set; }
    public TruckDayStatus UnitStatus { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? HelperId { get; set; }
    public Guid? SecondHelperId { get; set; }
    public string? Notes { get; set; }
}
