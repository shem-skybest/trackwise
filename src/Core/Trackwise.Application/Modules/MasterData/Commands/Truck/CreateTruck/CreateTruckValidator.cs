using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class CreateTruckValidator : TruckValidatorBase<CreateTruckCommand>
{
    public CreateTruckValidator()
    {
        // Number is optional on create - blank means "allocate one from the series".
        RuleFor(i => i.Number).MaximumLength(30);
        RuleFor(i => i.Name).MaximumLength(200);

        // Overriding the duplicate probe is allowed, but never silently: the reason is what a
        // reviewer reads months later when the duplicate turns out to be real after all.
        RuleFor(i => i.DistinctReason)
            .NotEmpty()
            .WithMessage("Give a reason why this is not one of the existing trucks.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDistinct);
    }
}
