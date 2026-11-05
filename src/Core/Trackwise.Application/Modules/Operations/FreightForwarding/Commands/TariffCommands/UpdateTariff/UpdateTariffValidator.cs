using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.UpdateTariff;

internal sealed class UpdateTariffValidator : AbstractValidator<UpdateTariffCommand>
{
    public UpdateTariffValidator()
    {
        RuleFor(i => i.Name).MaximumLength(200).When(i => i.Name is not null);
        RuleFor(i => i.OriginLane).MaximumLength(50).When(i => i.OriginLane is not null);
        RuleFor(i => i.DestinationLane).MaximumLength(50).When(i => i.DestinationLane is not null);
    }
}
