using FluentValidation;

namespace Trackwise.Application.Modules.MasterData.Commands;

internal abstract class TruckValidatorBase<T> : AbstractValidator<T> where T : ITruckFields
{
    protected TruckValidatorBase()
    {
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.VehicleCapacity).GreaterThanOrEqualTo(0).When(i => i.VehicleCapacity.HasValue);
        RuleFor(i => i.GrossVehicleWeightKg).InclusiveBetween(500, 60_000).When(i => i.GrossVehicleWeightKg.HasValue)
            .WithMessage("Gross vehicle weight must be between 500 and 60,000 kg.");
        RuleFor(i => i.Vin).MaximumLength(17);
        RuleFor(i => i.LicensePlate).MaximumLength(20);
        RuleFor(i => i.LicensePlateState).MaximumLength(10);
        RuleFor(i => i.AssetCode).MaximumLength(20);
        RuleFor(i => i.ConductionSticker).MaximumLength(20);
        RuleFor(i => i.EngineNumber).MaximumLength(30);
        RuleFor(i => i.Supplier).MaximumLength(100);
        RuleFor(i => i.Ownership).IsInEnum().When(i => i.Ownership.HasValue);
    }
}
