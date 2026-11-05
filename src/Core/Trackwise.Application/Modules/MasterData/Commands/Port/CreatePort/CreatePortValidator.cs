using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreatePortValidator : AbstractValidator<CreatePortCommand>
{
    public CreatePortValidator()
    {
        // Code is optional on create - blank means "allocate one from the series". No length rule
        // pinning it to five: legacy ports without a UN/LOCODE keep the code their records use.
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.CountryCode).NotEmpty().Length(2)
            .WithMessage("CountryCode must be a 2-letter ISO 3166-1 code.");
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.Address).NotNull().SetValidator(new AddressValidator());
        RuleFor(i => i.Notes).MaximumLength(2000);

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing ports.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
