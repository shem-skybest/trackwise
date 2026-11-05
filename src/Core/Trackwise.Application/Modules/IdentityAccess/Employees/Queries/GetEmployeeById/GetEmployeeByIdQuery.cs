using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Employees.Queries;

public class GetEmployeeByIdQuery : IQuery<Result<EmployeeDto>>
{
    public Guid UserId { get; set; }
}
