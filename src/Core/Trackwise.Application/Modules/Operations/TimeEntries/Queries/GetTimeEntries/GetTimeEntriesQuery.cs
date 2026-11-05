using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.TimeEntries.Queries;

public class GetTimeEntriesQuery : SearchableQuery, IQuery<PagedResult<TimeEntryDto>>
{
    public Guid? EmployeeId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Type { get; set; }
    public bool? IncludeLinkedToPayroll { get; set; }
}
