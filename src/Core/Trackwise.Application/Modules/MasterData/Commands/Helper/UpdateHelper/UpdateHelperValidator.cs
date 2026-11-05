using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateHelperValidator : AbstractValidator<UpdateHelperCommand>
{
    public UpdateHelperValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.PhoneNumber).MaximumLength(50);
        RuleFor(i => i.Address!).SetValidator(new AddressValidator()).When(i => i.Address is not null);
        RuleFor(i => i.Notes).MaximumLength(2000);
        RuleFor(i => i.EmploymentType).IsInEnum();
        RuleFor(i => i.HomeUnit).IsInEnum();

        // Retiring a helper things still depend on is allowed, but never silently.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a helper that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
