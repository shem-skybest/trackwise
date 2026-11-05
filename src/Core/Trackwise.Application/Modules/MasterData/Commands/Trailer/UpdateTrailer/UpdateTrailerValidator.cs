using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateTrailerValidator : AbstractValidator<UpdateTrailerCommand>
{
    public UpdateTrailerValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Type).IsInEnum();

        RuleFor(i => i.Vin)
            .Length(17)
            .WithMessage("A VIN is 17 characters.")
            .When(i => !string.IsNullOrWhiteSpace(i.Vin));

        RuleFor(i => i.LicensePlate).MaximumLength(20);
        RuleFor(i => i.LicensePlateState).MaximumLength(10);
        RuleFor(i => i.Make).MaximumLength(100);
        RuleFor(i => i.Model).MaximumLength(100);

        RuleFor(i => i.Year)
            .InclusiveBetween(1980, DateTime.UtcNow.Year + 1)
            .When(i => i.Year.HasValue);

        RuleFor(i => i.AxleCount).GreaterThan(0).When(i => i.AxleCount.HasValue);
        RuleFor(i => i.MaxLoadCapacityKg).GreaterThan(0).When(i => i.MaxLoadCapacityKg.HasValue);
        RuleFor(i => i.Notes).MaximumLength(2000);

        // Retiring a trailer things still depend on is allowed, but never silently.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a trailer that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
