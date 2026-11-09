using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.CreateTariff;

public class CreateTariffCommand : ICommand<Result<Guid>>
{
    public FreightMode Mode { get; set; }
    public ServiceLevel ServiceLevel { get; set; }
    public string? OriginLane { get; set; }
    public string? DestinationLane { get; set; }
    public Guid? ContainerTypeId { get; set; }
    public string Name { get; set; } = default!;
    public string Currency { get; set; } = default!;
    public Guid? CarrierId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public Guid? ContractId { get; set; }
    public Guid? CustomerId { get; set; }
}
