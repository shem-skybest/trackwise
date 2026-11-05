using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Platform.Portal.Queries;

internal sealed class GetCustomerUserByIdHandler(ITenantUnitOfWork tenantUow)
    : GetTenantEntityByIdHandler<GetCustomerUserByIdQuery, CustomerUser, CustomerUserDto>(tenantUow)
{
    protected override CustomerUserDto MapToDto(CustomerUser entity) => entity.ToDto();
}
