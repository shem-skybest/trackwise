using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateBranchValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchValidator()
    {
        // Code is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.ShortName).MaximumLength(20);
        RuleFor(i => i.Phone).MaximumLength(50);
        RuleFor(i => i.Email).EmailAddress().MaximumLength(256).When(i => !string.IsNullOrEmpty(i.Email));
        RuleFor(i => i.TaxId).MaximumLength(50);
        RuleFor(i => i.Address!).SetValidator(new AddressValidator()).When(i => i.Address is not null);

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing branches.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
