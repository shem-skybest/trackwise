using FluentValidation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.CreateTariff;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CreateTariffValidator : AbstractValidator<CreateTariffCommand>
{
    public CreateTariffValidator()
    {
        RuleFor(i => i.Mode).IsInEnum();
        RuleFor(i => i.ServiceLevel).IsInEnum();
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Currency).NotEmpty().MaximumLength(3);
        RuleFor(i => i.EffectiveFrom).NotEmpty();
    }
}
