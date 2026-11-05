using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal sealed class UpdateVesselValidator : AbstractValidator<UpdateVesselCommand>
{
    public UpdateVesselValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.CallSign).MaximumLength(20);
        RuleFor(i => i.Notes).MaximumLength(2000);

        RuleFor(i => i.ImoNumber)
            .Must(value => Domain.Entities.Vessel.NormalizeImoNumber(value)?.Length == 7)
            .WithMessage("An IMO number is seven digits.")
            .When(i => !string.IsNullOrWhiteSpace(i.ImoNumber));

        RuleFor(i => i.FlagCountryCode).Length(2)
            .WithMessage("FlagCountryCode must be a 2-letter ISO 3166-1 code.")
            .When(i => !string.IsNullOrWhiteSpace(i.FlagCountryCode));

        RuleFor(i => i.TeuCapacity).GreaterThan(0)
            .WithMessage("Leave TEU capacity empty rather than zero when it does not apply.")
            .When(i => i.TeuCapacity.HasValue);

        // Retiring a vessel things still depend on is allowed, but never silently.
        RuleFor(i => i.DependencyReason)
            .NotEmpty()
            .WithMessage("Give a reason for deactivating a vessel that is still in use.")
            .MaximumLength(500)
            .When(i => i.ConfirmedDespiteDependents && !i.IsActive);
    }
}
