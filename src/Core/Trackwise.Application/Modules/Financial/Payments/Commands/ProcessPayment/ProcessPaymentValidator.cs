using FluentValidation;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

internal sealed class ProcessPaymentValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentValidator()
    {
        RuleFor(i => i.PaymentId).NotEmpty();
    }
}
