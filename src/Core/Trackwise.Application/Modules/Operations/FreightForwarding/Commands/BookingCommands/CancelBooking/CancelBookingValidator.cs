using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.BookingCommands.CancelBooking;

internal sealed class CancelBookingValidator : AbstractValidator<CancelBookingCommand>
{
    public CancelBookingValidator()
    {
        RuleFor(i => i.Reason).MaximumLength(1000);
        RuleFor(i => i.Reason).NotEmpty().When(i => i.LossReason is null)
            .WithMessage("Say why the booking is cancelled.");
        RuleFor(i => i.LossReason).IsInEnum();
        RuleFor(i => i.CancelledByParty).IsInEnum();
    }
}
