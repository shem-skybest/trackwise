using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.DeactivateTariff;

public class DeactivateTariffCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string? Reason { get; set; }
}
