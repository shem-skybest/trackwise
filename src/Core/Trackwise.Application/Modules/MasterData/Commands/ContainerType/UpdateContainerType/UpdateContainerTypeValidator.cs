using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateContainerTypeValidator
    : ContainerTypeValidatorBase<UpdateContainerTypeCommand>
{
    public UpdateContainerTypeValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Notes).MaximumLength(2000);
        RuleFor(i => i.Category).IsInEnum();

        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for retiring a container type that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents);
    }
}
