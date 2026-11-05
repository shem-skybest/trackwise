using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.SupersedeTariff;

internal sealed class SupersedeTariffValidator : AbstractValidator<SupersedeTariffCommand>
{
    public SupersedeTariffValidator()
    {
        RuleFor(i => i.EffectiveFrom)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date));
        RuleFor(i => i.EffectiveTo)
            .GreaterThanOrEqualTo(i => i.EffectiveFrom)
            .When(i => i.EffectiveTo.HasValue);
        RuleFor(i => i.Reason).MaximumLength(500).When(i => i.Reason is not null);
    }
}
