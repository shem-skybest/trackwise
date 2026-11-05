using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateCurrencyValidator : CurrencyValidatorBase<UpdateCurrencyCommand>
{
    public UpdateCurrencyValidator()
    {
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for retiring a currency that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents);
    }
}
