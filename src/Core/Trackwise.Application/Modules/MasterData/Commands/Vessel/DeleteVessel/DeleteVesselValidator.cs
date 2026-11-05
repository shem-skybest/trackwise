using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class DeleteVesselValidator : AbstractValidator<DeleteVesselCommand>
{
    public DeleteVesselValidator()
    {
        RuleFor(i => i.Id).NotEmpty();

        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deleting a vessel that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents);
    }
}
