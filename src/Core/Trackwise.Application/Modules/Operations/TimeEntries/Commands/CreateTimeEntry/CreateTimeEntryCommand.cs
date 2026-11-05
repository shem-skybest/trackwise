using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.TimeEntries.Commands;

[RequiresFeature(TenantFeature.Timesheets)]
public class CreateTimeEntryCommand : ICommand<Result<Guid>>
{
    public required Guid EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal? TotalHours { get; set; }
    public TimeEntryType Type { get; set; } = TimeEntryType.Regular;
    public string? Notes { get; set; }
}
