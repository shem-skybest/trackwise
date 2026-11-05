using FluentValidation;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class SetDieselPriceValidator : AbstractValidator<SetDieselPriceCommand>
{
    public SetDieselPriceValidator()
    {
        // Wide on purpose: the cards run tiers from ₱85 to ₱180, and a bad week can leave them.
        RuleFor(i => i.PricePerLitre).InclusiveBetween(1, 1000);
        RuleFor(i => i.Notes).MaximumLength(500);
    }
}
