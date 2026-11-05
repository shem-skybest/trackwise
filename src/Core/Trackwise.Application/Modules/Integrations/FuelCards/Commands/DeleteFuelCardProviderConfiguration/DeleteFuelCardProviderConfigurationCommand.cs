using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Commands;

[RequiresFeature(TenantFeature.FuelCards)]
public class DeleteFuelCardProviderConfigurationCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }
}
