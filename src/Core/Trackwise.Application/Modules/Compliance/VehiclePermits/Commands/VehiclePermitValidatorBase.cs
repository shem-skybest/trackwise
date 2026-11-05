using FluentValidation;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

internal abstract class VehiclePermitValidatorBase<T> : AbstractValidator<T> where T : IVehiclePermitFields
{
    protected VehiclePermitValidatorBase()
    {
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.ExpiresOn).NotEmpty();
        RuleFor(i => i.IssuedOn)
            .LessThanOrEqualTo(i => i.ExpiresOn)
            .When(i => i.IssuedOn.HasValue)
            .WithMessage("The issue date must be on or before the expiry date.");
        RuleFor(i => i.Number).MaximumLength(64);
        RuleFor(i => i.Issuer).MaximumLength(100);
        RuleFor(i => i.Notes).MaximumLength(500);
        RuleFor(i => i.Cost).GreaterThanOrEqualTo(0).When(i => i.Cost.HasValue);
    }
}
