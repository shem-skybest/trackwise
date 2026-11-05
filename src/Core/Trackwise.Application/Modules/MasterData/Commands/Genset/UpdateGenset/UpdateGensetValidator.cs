using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateGensetValidator : AbstractValidator<UpdateGensetCommand>
{
    public UpdateGensetValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Status).IsInEnum();

        RuleFor(i => i.SerialNumber).MaximumLength(60);
        RuleFor(i => i.Make).MaximumLength(100);
        RuleFor(i => i.Model).MaximumLength(100);
        RuleFor(i => i.Notes).MaximumLength(2000);

        // Retiring a genset things still depend on is allowed, but never silently.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a genset that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
