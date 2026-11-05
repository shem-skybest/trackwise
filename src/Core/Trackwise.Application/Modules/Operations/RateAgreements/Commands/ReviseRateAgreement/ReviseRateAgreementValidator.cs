using FluentValidation;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

internal sealed class ReviseRateAgreementValidator : AbstractValidator<ReviseRateAgreementCommand>
{
    public ReviseRateAgreementValidator()
    {
        RuleFor(i => i.Name).NotEmpty().MaximumLength(200);
    }
}
