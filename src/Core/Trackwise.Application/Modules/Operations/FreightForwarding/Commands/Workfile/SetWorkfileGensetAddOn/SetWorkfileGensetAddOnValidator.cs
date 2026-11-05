using FluentValidation;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetWorkfileGensetAddOnValidator : AbstractValidator<SetWorkfileGensetAddOnCommand>
{
    public SetWorkfileGensetAddOnValidator()
    {
        RuleFor(i => i.BillingType).IsInEnum();
        RuleFor(i => i.Notes).MaximumLength(1000);

        RuleFor(i => i.HourlyRate)
            .NotNull()
            .When(i => i.BillingType == GensetBillingType.Itemized)
            .WithMessage("An itemized genset add-on requires an hourly rate.");

        RuleFor(i => i.FlatAmount)
            .NotNull()
            .When(i => i.BillingType == GensetBillingType.ProfitShare)
            .WithMessage("A profit-share genset add-on requires a flat amount.");
    }
}
