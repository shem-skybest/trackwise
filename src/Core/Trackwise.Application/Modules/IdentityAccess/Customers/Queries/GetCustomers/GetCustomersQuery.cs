using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Queries;

public class GetCustomersQuery : SearchableQuery, IQuery<PagedResult<CustomerDto>>;
