using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

/// <summary>
/// Starts a day's plan from another day's - most crews ride the same unit the next day. Only trucks
/// not yet planned on <see cref="ToDate" /> are filled, so a half-done plan is never overwritten, and
/// anyone already on another truck's crew that day, or absent, on leave or suspended, is left off the
/// copied row. Returns how many trucks were copied.
/// </summary>
public class CopyCrewPlanCommand : ICommand<Result<int>>
{
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
}
