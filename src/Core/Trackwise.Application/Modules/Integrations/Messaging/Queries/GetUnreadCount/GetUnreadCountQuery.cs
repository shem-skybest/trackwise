using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Messaging.Queries;

public class GetUnreadCountQuery : IQuery<Result<int>>
{
    public Guid EmployeeId { get; set; }
}
