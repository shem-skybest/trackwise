using FluentValidation;
using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateCurrencyValidator : CurrencyValidatorBase<CreateCurrencyCommand>
{
    public CreateCurrencyValidator()
    {
        // The one required code in the module. Checked on the normalised value so "php" and " PHP "
        // both pass and both store as PHP.
        RuleFor(i => i.Code)
            .NotEmpty()
            .WithMessage("A currency needs its ISO 4217 code - it is never allocated for you.")
            .Must(value => Currency.NormalizeCode(value) is { Length: 3 } code
                && code.All(char.IsAsciiLetterUpper))
            .WithMessage("An ISO 4217 code is three letters, e.g. PHP.");

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing currencies.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
