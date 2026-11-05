using FluentValidation;
using Trackwise.Domain.Entities;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// The rules create and edit share. One base rather than two copies, so an edit cannot reach a
/// state a create refuses.
/// </summary>
internal abstract class CurrencyValidatorBase<T> : AbstractValidator<T>
    where T : ICurrencyFields
{
    protected CurrencyValidatorBase()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Symbol).MaximumLength(10);
        RuleFor(x => x.Notes).MaximumLength(2000);

        // Checked on the normalised value, matching the server's own storage.
        RuleFor(x => x.NumericCode)
            .Must(value => Currency.NormalizeDigits(value) is { Length: 3 })
            .WithMessage("An ISO 4217 numeric code is three digits, e.g. 608.")
            .When(x => !string.IsNullOrWhiteSpace(x.NumericCode));

        // The range ISO 4217 actually uses. Two is the overwhelming default, zero is the yen and
        // the won, three is the dinar - and nothing subdivides further, so a larger number would be
        // a typo that silently changed how every total in that currency rounds.
        RuleFor(x => x.MinorUnits)
            .InclusiveBetween(0, 4)
            .WithMessage("Minor units are between 0 and 4 - 2 for most currencies, 0 for the yen.");
    }
}
