using FluentValidation;
using Trackwise.Application.Validators;

namespace Trackwise.Application.Modules.IdentityAccess.Customers.Commands;

internal sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(i => i.Name).NotEmpty();
        RuleFor(i => i.BillingCycle).IsInEnum();

        RuleFor(i => i.Address!)
            .SetValidator(new AddressValidator())
            .When(i => i.Address is not null);
    }
}
