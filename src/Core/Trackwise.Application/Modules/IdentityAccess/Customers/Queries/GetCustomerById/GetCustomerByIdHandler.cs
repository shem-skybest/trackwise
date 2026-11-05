using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Queries;

internal sealed class GetCustomerByIdHandler(ITenantUnitOfWork tenantUow)
    : GetTenantEntityByIdHandler<GetCustomerByIdQuery, Customer, CustomerDto>(tenantUow)
{
    protected override CustomerDto MapToDto(Customer entity) => entity.ToDto();
}
