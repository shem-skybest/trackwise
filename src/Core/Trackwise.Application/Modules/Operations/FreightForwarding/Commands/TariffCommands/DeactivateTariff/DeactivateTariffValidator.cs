using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.DeactivateTariff;

internal sealed class DeactivateTariffValidator : AbstractValidator<DeactivateTariffCommand>
{
    public DeactivateTariffValidator()
    {
        RuleFor(i => i.EffectiveTo)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .When(i => i.EffectiveTo.HasValue);
        RuleFor(i => i.Reason).MaximumLength(500).When(i => i.Reason is not null);
    }
}
