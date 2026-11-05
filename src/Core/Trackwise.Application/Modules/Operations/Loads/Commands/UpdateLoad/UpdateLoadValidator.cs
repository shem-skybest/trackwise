using FluentValidation;
using Trackwise.Application.Modules.Operations.Loads.Constants;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.Operations.Loads.Commands;

internal sealed class UpdateLoadValidator : AbstractValidator<UpdateLoadCommand>
{
    public UpdateLoadValidator()
    {
        RuleFor(i => i.Distance).GreaterThan(0);
        RuleFor(i => i.DeliveryCost)
            .GreaterThan(LoadConstants.MinDeliveryCost)
            .LessThan(LoadConstants.MaxDeliveryCost);

        RuleFor(i => i.OriginAddress!)
            .SetValidator(new AddressValidator())
            .When(i => i.OriginAddress is not null);

        RuleFor(i => i.DestinationAddress!)
            .SetValidator(new AddressValidator())
            .When(i => i.DestinationAddress is not null);

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
