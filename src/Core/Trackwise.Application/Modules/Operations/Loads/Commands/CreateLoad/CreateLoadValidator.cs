using FluentValidation;
using Trackwise.Application.Modules.Operations.Loads.Constants;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

internal sealed class CreateLoadValidator : AbstractValidator<CreateLoadCommand>
{
    public CreateLoadValidator()
    {
        RuleFor(i => i.Name).NotEmpty();
        RuleFor(i => i.Type).IsInEnum();
        RuleFor(i => i.AssignedDispatcherId).NotEmpty();
        // AssignedTruckId is optional - load can be created without truck assignment (e.g., from load board)
        RuleFor(i => i.OriginAddress).NotNull().SetValidator(new AddressValidator());
        RuleFor(i => i.OriginLocation).NotEmpty();
        RuleFor(i => i.DestinationAddress).NotNull().SetValidator(new AddressValidator());
        RuleFor(i => i.DestinationLocation).NotEmpty();
        RuleFor(i => i.Distance).GreaterThan(0);
        RuleFor(i => i.DeliveryCost)
            .GreaterThan(LoadConstants.MinDeliveryCost)
            .LessThan(LoadConstants.MaxDeliveryCost);

        When(i => i.RequestedPickupDate.HasValue && i.RequestedDeliveryDate.HasValue, () =>
        {
            RuleFor(i => i.RequestedDeliveryDate!.Value)
                .GreaterThanOrEqualTo(i => i.RequestedPickupDate!.Value)
                .WithMessage("Requested delivery date must be on or after requested pickup date.");
        });

        RuleFor(i => i.Commodity).MaximumLength(100);
        RuleFor(i => i.PlannedBoxes).GreaterThanOrEqualTo(0).When(i => i.PlannedBoxes.HasValue);
        RuleFor(i => i.PlannedBags).GreaterThanOrEqualTo(0).When(i => i.PlannedBags.HasValue);
    }
}
