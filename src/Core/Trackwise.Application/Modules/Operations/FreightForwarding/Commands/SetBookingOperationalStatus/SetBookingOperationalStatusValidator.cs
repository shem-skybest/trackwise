using FluentValidation;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

internal sealed class SetBookingOperationalStatusValidator : AbstractValidator<SetBookingOperationalStatusCommand>
{
    public SetBookingOperationalStatusValidator()
    {
        RuleFor(i => i.Id).NotEmpty();
        RuleFor(i => i.Status).IsInEnum();
        RuleFor(i => i.LossReason).IsInEnum();
    }
}
