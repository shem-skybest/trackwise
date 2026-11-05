using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateGensetValidator : AbstractValidator<CreateGensetCommand>
{
    public CreateGensetValidator()
    {
        // Code is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Code).MaximumLength(30);
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Status).IsInEnum();

        RuleFor(i => i.SerialNumber).MaximumLength(60);
        RuleFor(i => i.Make).MaximumLength(100);
        RuleFor(i => i.Model).MaximumLength(100);
        RuleFor(i => i.Notes).MaximumLength(2000);

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing gensets.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
