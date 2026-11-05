using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateBranchValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.ShortName).MaximumLength(20);
        RuleFor(i => i.Phone).MaximumLength(50);
        RuleFor(i => i.Email).EmailAddress().MaximumLength(256).When(i => !string.IsNullOrEmpty(i.Email));
        RuleFor(i => i.TaxId).MaximumLength(50);
        RuleFor(i => i.Address!).SetValidator(new AddressValidator()).When(i => i.Address is not null);

        // Retiring a branch things still depend on is allowed, but never silently - the reason is
        // what a reviewer reads when those dependents turn out to have mattered.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a branch that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
