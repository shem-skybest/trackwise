using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteLocationValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationValidator()
    {
        RuleFor(i => i.Id).NotEmpty();

        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deleting a location that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents);
    }
}
