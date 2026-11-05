using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.ConfirmBooking;

internal sealed class ConfirmBookingValidator : AbstractValidator<ConfirmBookingCommand>
{
    public ConfirmBookingValidator()
    {
        RuleFor(i => i.CarrierReference).MaximumLength(200);
    }
}
