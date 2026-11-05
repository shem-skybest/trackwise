using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Queries;

public class GetEmployeesQuery : SearchableQuery, IQuery<PagedResult<EmployeeDto>>
{
    /// <summary>
    /// Exact tenant role names to include (e.g. <c>tenant.driver</c>); empty returns every role.
    /// A list because "who can drive" spans Driver and Owner - an owner-operator drives their own.
    /// </summary>
    public string[] Roles { get; set; } = [];

    /// <summary>
    /// Only active employees who can be put on a truck this day: anyone whose crew status for it is
    /// absent, on leave, suspended, training or lent out is left out. For the crew pickers.
    /// </summary>
    public DateOnly? AvailableOn { get; set; }
}
