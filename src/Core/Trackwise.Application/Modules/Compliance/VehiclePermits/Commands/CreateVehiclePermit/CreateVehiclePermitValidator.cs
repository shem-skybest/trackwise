using FluentValidation;

namespace Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;

internal sealed class CreateVehiclePermitValidator : VehiclePermitValidatorBase<CreateVehiclePermitCommand>
{
    public CreateVehiclePermitValidator()
    {
        RuleFor(i => i)
            .Must(i => i.TruckId.HasValue != i.TrailerId.HasValue)
            .WithName("Vehicle")
            .WithMessage("A paper belongs to exactly one vehicle: give a truck or a trailer, not both.");
    }
}
