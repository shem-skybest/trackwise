using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateCommodityValidator : AbstractValidator<CreateCommodityCommand>
{
    public CreateCommodityValidator()
    {
        // Code is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Description).MaximumLength(1000);
        RuleFor(i => i.Notes).MaximumLength(2000);

        // Checked on the normalised value: HS codes are written with dots on customs paperwork,
        // and rejecting "8471.30" would teach stewards to strip punctuation by hand. Six digits is
        // the international heading; countries subdivide to eight or ten.
        RuleFor(i => i.HsCode)
            .Must(value => Domain.Entities.Commodity.NormalizeDigits(value) is { Length: >= 6 and <= 10 })
            .WithMessage("An HS code is between six and ten digits.")
            .When(i => !string.IsNullOrWhiteSpace(i.HsCode));

        RuleFor(i => i.UnNumber)
            .Must(value => Domain.Entities.Commodity.NormalizeDigits(value)?.Length == 4)
            .WithMessage("A UN number is four digits.")
            .When(i => !string.IsNullOrWhiteSpace(i.UnNumber));

        // One-directional on purpose. A UN number without the hazardous flag is a contradiction, but
        // a hazardous commodity awaiting classification is an ordinary state to be in - requiring
        // the number would push the steward into inventing one.
        RuleFor(i => i.UnNumber)
            .Empty()
            .WithMessage("A UN number only applies to a hazardous commodity. Tick hazardous, or clear it.")
            .When(i => !i.IsHazardous);

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing commodities.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
