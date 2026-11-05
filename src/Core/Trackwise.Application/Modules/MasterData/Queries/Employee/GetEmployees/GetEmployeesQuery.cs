using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Queries;

public class GetEmployeesQuery : SearchableQuery, IQuery<PagedResult<EmployeeDto>>
{
    public bool? IsActive { get; set; }

    /// <summary>Employees scoped to one branch. Records that apply everywhere are always included.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>
    /// Surfaces soft-deleted records so a steward can review or restore them. Off by default - the
    /// global query filter is what keeps deleted rows out of every other screen.
    /// </summary>
    public bool IncludeDeleted { get; set; }

    /// <summary>
    /// Incremental pull for client systems: return only records touched since this watermark.
    /// </summary>
    public DateTime? UpdatedSince { get; set; }
}
