using FluentValidation;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class CreateRateAgreementValidator : RateAgreementTermsValidator<CreateRateAgreementCommand>
{
    public CreateRateAgreementValidator()
    {
        RuleFor(i => i.CustomerId).NotEmpty();
    }
}
