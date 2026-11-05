using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Queries;

public class GetCustomerByIdQuery : IQuery<Result<CustomerDto>>, IHaveId
{
    public Guid Id { get; set; }
}
