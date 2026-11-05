using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetNextBookingNumberValidator : AbstractValidator<SetNextBookingNumberCommand>
{
    public SetNextBookingNumberValidator()
    {
        RuleFor(x => x.NextNumber).InclusiveBetween(1, 999_999);
    }
}
