using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Queries;

[RequiresFeature(TenantFeature.FuelCards)]
public class GetFuelCardProviderConfigurationsQuery : IQuery<Result<List<FuelCardProviderConfigurationDto>>>
{
}
