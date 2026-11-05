using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeletePortValidator : AbstractValidator<DeletePortCommand>
{
    public DeletePortValidator()
    {
        RuleFor(i => i.Id).NotEmpty();

        // Deleting a port things still depend on is allowed, but never silently - the reason is
        // what a reviewer reads when those dependents turn out to have mattered.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deleting a port that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents);
    }
}
