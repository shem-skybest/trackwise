using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.TariffCommands.AddTariffRate;

internal sealed class AddTariffRateValidator : AbstractValidator<AddTariffRateCommand>
{
    public AddTariffRateValidator()
    {
        RuleFor(i => i.ChargeCodeId).NotEmpty();
        RuleFor(i => i.PricingModel).IsInEnum();
        RuleFor(i => i.BaseAmount).GreaterThanOrEqualTo(0);
        RuleFor(i => i.Unit).NotEmpty().MaximumLength(20);
        RuleFor(i => i.Minimum).GreaterThanOrEqualTo(0).When(i => i.Minimum.HasValue);
        RuleFor(i => i.Maximum).GreaterThanOrEqualTo(0).When(i => i.Maximum.HasValue);
        RuleFor(i => i.Maximum)
            .GreaterThanOrEqualTo(i => i.Minimum!.Value)
            .When(i => i.Minimum.HasValue && i.Maximum.HasValue);
        RuleFor(i => i.Percentage).InclusiveBetween(0, 100).When(i => i.Percentage.HasValue);
        RuleFor(i => i.FreeDays).GreaterThanOrEqualTo(0).When(i => i.FreeDays.HasValue);
    }
}
