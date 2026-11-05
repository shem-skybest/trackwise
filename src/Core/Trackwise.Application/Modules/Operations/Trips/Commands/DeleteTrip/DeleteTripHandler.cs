using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Operations.Trips.Commands;

internal sealed class DeleteTripHandler(ITenantUnitOfWork tenantUow)
    : DeleteTenantEntityHandler<DeleteTripCommand, Trip>(tenantUow);
