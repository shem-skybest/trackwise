using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteGensetValidator : AbstractValidator<DeleteGensetCommand>
{
    public DeleteGensetValidator()
    {
        RuleFor(i => i.Id).NotEmpty();

        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deleting a genset that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents);
    }
}
