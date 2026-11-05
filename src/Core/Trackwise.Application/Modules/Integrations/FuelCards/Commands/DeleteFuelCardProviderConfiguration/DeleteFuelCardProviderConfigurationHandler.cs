using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Commands;

internal sealed class DeleteFuelCardProviderConfigurationHandler(ITenantUnitOfWork tenantUow)
    : DeleteTenantEntityHandler<DeleteFuelCardProviderConfigurationCommand, FuelCardProviderConfiguration>(tenantUow);
