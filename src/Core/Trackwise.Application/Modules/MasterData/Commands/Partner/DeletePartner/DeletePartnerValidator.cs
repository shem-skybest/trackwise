using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeletePartnerValidator : AbstractValidator<DeletePartnerCommand>
{
    public DeletePartnerValidator()
    {
        RuleFor(i => i.Id).NotEmpty();

        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deleting a partner that open files still name.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents);
    }
}
