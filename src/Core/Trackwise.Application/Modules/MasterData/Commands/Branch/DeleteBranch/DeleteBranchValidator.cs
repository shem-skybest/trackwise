using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteBranchValidator : AbstractValidator<DeleteBranchCommand>
{
    public DeleteBranchValidator()
    {
        RuleFor(i => i.Id).NotEmpty();

        // Deleting a branch things still depend on is allowed, but never silently - the reason is
        // what a reviewer reads when those dependents turn out to have mattered.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deleting a branch that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents);
    }
}
