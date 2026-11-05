using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Queries;

internal sealed class GetBusinessPartnerByIdHandler(ITenantUnitOfWork tenantUow)
    : GetTenantEntityByIdHandler<GetBusinessPartnerByIdQuery, BusinessPartner, BusinessPartnerDto>(tenantUow)
{
    protected override BusinessPartnerDto MapToDto(BusinessPartner entity) => entity.ToDto();
}
