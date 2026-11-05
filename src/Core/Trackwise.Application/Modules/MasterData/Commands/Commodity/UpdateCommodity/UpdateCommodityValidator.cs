using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateCommodityValidator : AbstractValidator<UpdateCommodityCommand>
{
    public UpdateCommodityValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Description).MaximumLength(1000);
        RuleFor(i => i.Notes).MaximumLength(2000);

        RuleFor(i => i.HsCode)
            .Must(value => Domain.Entities.Commodity.NormalizeDigits(value) is { Length: >= 6 and <= 10 })
            .WithMessage("An HS code is between six and ten digits.")
            .When(i => !string.IsNullOrWhiteSpace(i.HsCode));

        RuleFor(i => i.UnNumber)
            .Must(value => Domain.Entities.Commodity.NormalizeDigits(value)?.Length == 4)
            .WithMessage("A UN number is four digits.")
            .When(i => !string.IsNullOrWhiteSpace(i.UnNumber));

        RuleFor(i => i.UnNumber)
            .Empty()
            .WithMessage("A UN number only applies to a hazardous commodity. Tick hazardous, or clear it.")
            .When(i => !i.IsHazardous);

        // Retiring a commodity things still depend on is allowed, but never silently.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a commodity that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
