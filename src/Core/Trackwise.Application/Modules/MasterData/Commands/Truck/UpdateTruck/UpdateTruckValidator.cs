using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateTruckValidator : TruckValidatorBase<UpdateTruckCommand>
{
    public UpdateTruckValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Status).IsInEnum();

        // Retiring a truck things still depend on is allowed, but never silently - the reason is
        // what a reviewer reads when those dependents turn out to have mattered.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a truck that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
