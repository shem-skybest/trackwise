using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Integrations.Negotiation.Commands;

internal sealed class DeleteLaneRateFloorHandler(ITenantUnitOfWork tenantUow)
    : DeleteTenantEntityHandler<DeleteLaneRateFloorCommand, LaneRateFloor>(tenantUow);
