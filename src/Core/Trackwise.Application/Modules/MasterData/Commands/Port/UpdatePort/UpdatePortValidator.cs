using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdatePortValidator : AbstractValidator<UpdatePortCommand>
{
    public UpdatePortValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.CountryCode).NotEmpty().Length(2)
            .WithMessage("CountryCode must be a 2-letter ISO 3166-1 code.");
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.Address).NotNull().SetValidator(new AddressValidator());
        RuleFor(i => i.Notes).MaximumLength(2000);

        // Retiring a port things still depend on is allowed, but never silently - the reason is
        // what a reviewer reads when those dependents turn out to have mattered.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a port that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
