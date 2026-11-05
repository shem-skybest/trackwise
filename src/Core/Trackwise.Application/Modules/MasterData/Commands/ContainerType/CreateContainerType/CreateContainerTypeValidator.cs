using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateContainerTypeValidator
    : ContainerTypeValidatorBase<CreateContainerTypeCommand>
{
    public CreateContainerTypeValidator()
    {
        // Code is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Notes).MaximumLength(2000);
        RuleFor(i => i.Category).IsInEnum();

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing container types.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
