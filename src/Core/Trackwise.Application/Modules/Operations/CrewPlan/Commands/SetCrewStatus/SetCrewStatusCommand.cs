using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.CrewPlan.Commands;

/// <summary>
/// Sets a driver's or helper's status for a day, or for every day through <see cref="UntilDate" />
/// (a week's leave, a suspension). Absent, on leave and suspended also take the person off any truck
/// they were planned on those days; trips already created keep their crew.
/// </summary>
public class SetCrewStatusCommand : ICommand<Result<CrewStatusChangeDto>>
{
    /// <summary>The driver's employee id or the helper's id.</summary>
    public Guid PersonId { get; set; }

    public DateOnly Date { get; set; }

    /// <summary>The last day the status applies, inclusive; just <see cref="Date" /> when omitted.</summary>
    public DateOnly? UntilDate { get; set; }

    public CrewDayStatus Status { get; set; }
    public string? Notes { get; set; }
}
