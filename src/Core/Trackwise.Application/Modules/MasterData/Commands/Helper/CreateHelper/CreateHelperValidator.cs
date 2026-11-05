using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateHelperValidator : AbstractValidator<CreateHelperCommand>
{
    public CreateHelperValidator()
    {
        // Code is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.PhoneNumber).MaximumLength(50);
        RuleFor(i => i.Address!).SetValidator(new AddressValidator()).When(i => i.Address is not null);
        RuleFor(i => i.Notes).MaximumLength(2000);
        RuleFor(i => i.EmploymentType).IsInEnum();
        RuleFor(i => i.HomeUnit).IsInEnum();

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing helpers.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
