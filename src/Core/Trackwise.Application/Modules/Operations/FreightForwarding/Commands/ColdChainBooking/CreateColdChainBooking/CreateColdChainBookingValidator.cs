using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class CreateColdChainBookingValidator : ColdChainBookingPlanValidator<CreateColdChainBookingCommand>
{
    public CreateColdChainBookingValidator()
    {
        RuleFor(i => i.CustomerId).NotEmpty();
        RuleFor(i => i.ReferenceNumber).MaximumLength(30);
    }
}
