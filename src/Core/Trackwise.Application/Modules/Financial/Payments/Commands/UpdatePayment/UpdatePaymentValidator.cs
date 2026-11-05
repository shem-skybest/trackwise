using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.Financial.Payments.Commands;

internal sealed class UpdatePaymentValidator : AbstractValidator<UpdatePaymentCommand>
{
    public UpdatePaymentValidator()
    {
        RuleFor(i => i.BillingAddress!)
            .SetValidator(new AddressValidator())
            .When(i => i.BillingAddress is not null);
    }
}
