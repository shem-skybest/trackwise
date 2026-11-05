using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetBookingReferenceNumberValidator : AbstractValidator<SetBookingReferenceNumberCommand>
{
    public SetBookingReferenceNumberValidator()
    {
        RuleFor(x => x.ReferenceNumber).NotEmpty().MaximumLength(30);
    }
}
