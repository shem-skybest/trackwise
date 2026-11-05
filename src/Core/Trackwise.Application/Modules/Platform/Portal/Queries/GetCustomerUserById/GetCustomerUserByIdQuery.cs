using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Portal.Queries;

/// <summary>
/// Query to get a CustomerUser by ID.
/// </summary>
public class GetCustomerUserByIdQuery : IQuery<Result<CustomerUserDto>>, IHaveId
{
    /// <summary>
    /// The customer user ID.
    /// </summary>
    public required Guid Id { get; set; }
}
