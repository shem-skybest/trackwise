using Trackwise.Domain.Entities.Safety;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Safety.Queries;

internal sealed class GetDriverBehaviorEventByIdHandler(ITenantUnitOfWork tenantUow)
    : GetTenantEntityByIdHandler<GetDriverBehaviorEventByIdQuery, DriverBehaviorEvent, DriverBehaviorEventDto>(tenantUow)
{
    protected override DriverBehaviorEventDto MapToDto(DriverBehaviorEvent entity) => entity.ToDto();
}
