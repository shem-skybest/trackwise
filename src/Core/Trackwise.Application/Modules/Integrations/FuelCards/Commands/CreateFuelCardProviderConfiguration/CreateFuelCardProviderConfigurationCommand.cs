using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Commands;

[RequiresFeature(TenantFeature.FuelCards)]
public class CreateFuelCardProviderConfigurationCommand : ICommand<Result>
{
    public FuelCardProviderType ProviderType { get; set; }
    public required string ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? ExternalAccountId { get; set; }
}
